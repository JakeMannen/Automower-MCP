using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;

namespace AutomowerMcp.Services;

/// <summary>
/// DelegatingHandler that uses <see cref="HusqvarnaTokenService"/> to acquire and cache
/// a Husqvarna OAuth2 access token, then injects the required authentication headers
/// into every outbound request.
/// </summary>
public class HusqvarnaAuthHandler : DelegatingHandler
{
    private readonly HttpClient _authClient;
    private readonly HusqvarnaTokenService _tokenService;
    private readonly ILogger<HusqvarnaAuthHandler> _logger;

    public HusqvarnaAuthHandler(
        HttpClient authClient,
        HusqvarnaTokenService tokenService,
        ILogger<HusqvarnaAuthHandler> logger)
    {
        _authClient = authClient;
        _tokenService = tokenService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var token = await _tokenService.GetAccessTokenAsync(ct);

        var (apiKey, _) = GetCredentials();

        request.Headers.Add("X-Api-Key", apiKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Authorization-Provider", "husqvarna");

        return await base.SendAsync(request, ct);
    }

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
}
