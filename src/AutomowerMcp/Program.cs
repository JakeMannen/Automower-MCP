using AutomowerMcp;
using AutomowerMcp.Services;
using ModelContextProtocol;

using Microsoft.Extensions.Logging;

using ILoggerFactory loggerFactory =
    LoggerFactory.Create(builder =>
        builder.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        }));

ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

var transport = Environment.GetEnvironmentVariable("MCP_TRANSPORT") ?? "stdio";

try
{
    
    if (transport.Equals("http", StringComparison.OrdinalIgnoreCase))
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Logging.AddSimpleConsole(options => 
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });

        RegisterCommonServices(builder.Services);

        builder.Services
            .AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly();

        var app = builder.Build();
        app.MapGet("/health", () => Results.Ok());
        app.MapMcp();
        await app.RunAsync();
    }
    else
    {
        var builder = Host.CreateApplicationBuilder(args);
        // Route all logs to stderr so stdout is reserved for MCP protocol traffic
        builder.Logging.AddConsole(options =>
            options.LogToStandardErrorThreshold = LogLevel.Trace);

        RegisterCommonServices(builder.Services);

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        await builder.Build().RunAsync();
    }
}
catch (McpProtocolException ex)
{
    await Console.Error.WriteLineAsync(
        $"[automower-mcp] MCP protocol error ({ex.ErrorCode}): {ex.Message}");
    return 2;
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync(
        $"[automower-mcp] Fatal error: {ex}");
    return 1;
}

return 0;

static void RegisterCommonServices(IServiceCollection services)
{
    // The auth handler acquires/caches OAuth2 tokens and injects all required headers.
    // Must be singleton so the token cache survives across requests.
    services.AddSingleton<HusqvarnaAuthHandler>();

    // Named HttpClient for the Husqvarna Automower API
    services.AddHttpClient("automower", client =>
    {
        client.BaseAddress = new Uri(ApiUrls.AutomowerBaseAddress);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<HusqvarnaAuthHandler>();

    services.AddSingleton<IAutomowerApiService, AutomowerApiService>();
}

