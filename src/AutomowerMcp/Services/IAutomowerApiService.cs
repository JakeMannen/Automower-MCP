namespace AutomowerMcp.Services;

public interface IAutomowerApiService
{
    Task<string> GetAsync(string path, CancellationToken ct = default);
    Task<string> PostAsync(string path, object? body = null, CancellationToken ct = default);
    Task<string> PatchAsync(string path, object body, CancellationToken ct = default);
}
