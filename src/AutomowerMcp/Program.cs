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
    logger.LogError(ex, "[automower-mcp] MCP protocol error ({0}): {1}", ex.ErrorCode, ex.Message);
    return 2;
}
catch (Exception ex)
{
    logger.LogError(ex, "[automower-mcp] Fatal error: {0}", ex.Message);
    return 1;
}

return 0;

static void RegisterCommonServices(IServiceCollection services)
{
    // Register the token service as a singleton to cache the access token across requests
    services.AddSingleton<HusqvarnaTokenService>();

    // Named HttpClient for the Husqvarna Automower API
    // Factory function creates a new AuthHandler instance per request (required by DelegatingHandler)
    services.AddHttpClient("automower", client =>
    {
        client.BaseAddress = new Uri(ApiUrls.AutomowerBaseAddress);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler(sp =>
    {
        var authClient = sp.GetRequiredService<HttpClient>();
        var tokenService = sp.GetRequiredService<HusqvarnaTokenService>();
        var logger = sp.GetRequiredService<ILogger<HusqvarnaAuthHandler>>();
        return new HusqvarnaAuthHandler(authClient, tokenService, logger);
    });

    services.AddSingleton<IAutomowerApiService, AutomowerApiService>();
}

