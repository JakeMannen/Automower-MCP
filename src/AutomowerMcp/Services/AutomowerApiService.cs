using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;

namespace AutomowerMcp.Services;

/// <summary>
/// Thin wrapper around the Husqvarna Automower Connect REST API.
/// Authentication is handled automatically by <see cref="HusqvarnaAuthHandler"/>,
/// which is registered as a delegating handler on the "automower" HttpClient.
/// </summary>
public class AutomowerApiService(
    IHttpClientFactory httpClientFactory,
    ILogger<AutomowerApiService> logger) : IAutomowerApiService
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

    /// <summary>
    /// Sends a GET request to the Automower Connect API to retrieve resources.
    /// Used for operations like fetching mower status, retrieving work area details, or getting calendar schedules.
    /// </summary>
    /// <param name="path">The API endpoint path to send the GET request to.</param>
    /// <param name="ct">Cancellation token for the async operation.</param>
    /// <returns>The enriched response body from the API.</returns>    public async Task<string> GetAsync(string path, CancellationToken ct = default)
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
        {            logger.LogError(ex, "GET {Path} failed", path);            throw new McpException($"GET {path} failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Sends a POST request to the Automower Connect API to create or update resources.
    /// Used for operations like creating new work areas, adding stay-out zones, or submitting calendar tasks.
    /// The request body (if provided) is serialized to JSON with media type "application/vnd.api+json".
    /// </summary>
    /// <param name="path">The API endpoint path to send the POST request to.</param>
    /// <param name="body">Optional JSON-serializable object containing the request payload.</param>
    /// <param name="ct">Cancellation token for the async operation.</param>
    /// <returns>The enriched response body from the API.</returns>
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
            logger.LogError(ex, "POST {Path} failed", path);
            throw new McpException($"POST {path} failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Sends a PATCH request to the Automower Connect API to partially update resources.
    /// Used for operations like updating mower settings, modifying work area properties, or adjusting calendar schedules.
    /// The request body is serialized to JSON with media type "application/vnd.api+json".
    /// </summary>
    /// <param name="path">The API endpoint path to send the PATCH request to.</param>
    /// <param name="body">JSON-serializable object containing the partial update payload.</param>
    /// <param name="ct">Cancellation token for the async operation.</param>
    /// <returns>The enriched response body from the API.</returns>
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
            logger.LogError(ex, "PATCH {Path} failed", path);
            throw new McpException($"PATCH {path} failed: {ex.Message}", ex);
        }
    }
}


