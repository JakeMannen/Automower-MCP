using System.Net.Http.Headers;
using System.Text.Json;
using ModelContextProtocol;

namespace AutomowerMcp.Services;

/// <summary>
/// DelegatingHandler that transparently acquires and caches a Husqvarna OAuth2
/// access token via the client_credentials grant, then injects the required
/// authentication headers into every outbound request.
/// </summary>
public class HusqvarnaAuthHandler : DelegatingHandler
{
    private readonly HttpClient _authClient = new();
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static (string apiKey, string secret) GetCredentials()
    {
        var apiKey = Environment.GetEnvironmentVariable("HUSQVARNA_API_KEY");
        var secret = Environment.GetEnvironmentVariable("HUSQVARNA_APPLICATION_SECRET");

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new McpException(
                "HUSQVARNA_API_KEY environment variable is not set. " +
                "Set it to the Application Key (client_id) from developer.husqvarnagroup.cloud.");

        if (string.IsNullOrWhiteSpace(secret))
            throw new McpException(
                "HUSQVARNA_APPLICATION_SECRET environment variable is not set. " +
                "Set it to the Application Secret (client_secret) from developer.husqvarnagroup.cloud.");

        return (apiKey, secret);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiry)
            return _cachedToken;

        await _lock.WaitAsync(ct);
        try
        {
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiry)
                return _cachedToken;

            var (apiKey, secret) = GetCredentials();

            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "client_credentials",
                ["client_id"]     = apiKey,
                ["client_secret"] = secret,
            });

            try
            {
                using var response = await _authClient.PostAsync(
                    ApiUrls.AuthTokenEndpoint, form, ct);
                response.EnsureSuccessStatusCode();

                using var doc = await JsonDocument.ParseAsync(
                    await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

                _cachedToken = doc.RootElement.GetProperty("access_token").GetString()
                    ?? throw new McpException("Husqvarna auth response did not contain an access_token.");

                // Husqvarna tokens expire in 3600 s; subtract a 60-second buffer
                var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
                _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60);

                return _cachedToken;
            }
            catch (McpException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                throw new McpException(
                    $"Failed to reach the Husqvarna authentication endpoint: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new McpException(
                    $"Husqvarna auth response could not be parsed: {ex.Message}", ex);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var (apiKey, _) = GetCredentials();
        var token = await GetAccessTokenAsync(ct);

        request.Headers.Add("X-Api-Key", apiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Authorization-Provider", "husqvarna");

        return await base.SendAsync(request, ct);
    }
}
