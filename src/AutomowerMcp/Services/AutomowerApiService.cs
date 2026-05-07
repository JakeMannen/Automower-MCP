using System.Text;
using System.Text.Json;
using ModelContextProtocol;

namespace AutomowerMcp.Services;

/// <summary>
/// Thin wrapper around the Husqvarna Automower Connect REST API.
/// Authentication is handled automatically by <see cref="HusqvarnaAuthHandler"/>,
/// which is registered as a delegating handler on the "automower" HttpClient.
/// </summary>
public class AutomowerApiService(IHttpClientFactory httpClientFactory) : IAutomowerApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static async Task<string> ReadResponseAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new McpException(
                $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {body}");

        return string.IsNullOrWhiteSpace(body) ? "{\"result\":\"success\"}" : body;
    }

    public async Task<string> GetAsync(string path, CancellationToken ct = default)
    {
        try
        {
            using var client = httpClientFactory.CreateClient("automower");
            using var response = await client.GetAsync(path, ct);
            return ResponseEnricher.Enrich(await ReadResponseAsync(response));
        }
        catch (McpException) { throw; }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            throw new McpException($"GET {path} failed: {ex.Message}", ex);
        }
    }

    public async Task<string> PostAsync(string path, object? body = null, CancellationToken ct = default)
    {
        try
        {
            using var client = httpClientFactory.CreateClient("automower");
            HttpContent? content = body is not null
                ? new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/vnd.api+json")
                : null;
            using var response = await client.PostAsync(path, content, ct);
            return await ReadResponseAsync(response);
        }
        catch (McpException) { throw; }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            throw new McpException($"POST {path} failed: {ex.Message}", ex);
        }
    }

    public async Task<string> PatchAsync(string path, object body, CancellationToken ct = default)
    {
        try
        {
            using var client = httpClientFactory.CreateClient("automower");
            var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/vnd.api+json");
            using var response = await client.PatchAsync(path, content, ct);
            return await ReadResponseAsync(response);
        }
        catch (McpException) { throw; }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            throw new McpException($"PATCH {path} failed: {ex.Message}", ex);
        }
    }
}


