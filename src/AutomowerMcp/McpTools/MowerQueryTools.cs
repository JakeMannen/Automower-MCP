using AutomowerMcp.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace AutomowerMcp.McpTools;

/// <summary>
/// MCP tools that map to the read (GET) endpoints of the Automower Connect API.
/// </summary>
[McpServerToolType]
public class MowerQueryTools(IAutomowerApiService api)
{
    [McpServerTool]
    [Description(
        "List all Automower® mowers linked to the authenticated user. " +
        "Returns an array of mower objects. Each mower includes errorCode, errorCodeDescription, " +
        "activity, activityDescription, state, stateDescription, mode, modeDescription, " +
        "battery percentage, GPS positions, settings, statistics, stay-out zones, and work areas.")]
    public Task<string> ListMowers(CancellationToken ct)
        => api.GetAsync("mowers", ct);

    [McpServerTool]
    [Description(
        "Get full details for a single Automower® by its UUID. " +
        "Returns the same enriched data as ListMowers for one mower, including " +
        "errorCodeDescription, modeDescription, activityDescription, and stateDescription. " +
        "Use this to refresh status or when you already know the mower id.")]
    public Task<string> GetMower(
        [Description("UUID of the mower, e.g. 256b2365-33a7-46fe-a9fb-e67e84f4ac01")] string mowerId,
        CancellationToken ct)
        => api.GetAsync($"mowers/{mowerId}", ct);

    [McpServerTool]
    [Description(
        "Get the last messages/events for a mower (up to 50). " +
        "Each message includes: errorCode, errorCodeDescription, severity (FATAL/ERROR/WARNING/INFO/DEBUG), " +
        "timestamp (ms since epoch, local mower time), and optional GPS coordinates where the event occurred.")]
    public Task<string> GetMowerMessages(
        [Description("UUID of the mower")] string mowerId,
        CancellationToken ct)
        => api.GetAsync($"mowers/{mowerId}/messages", ct);

    [McpServerTool]
    [Description(
        "Get all stay-out zones configured for a mower. " +
        "Stay-out zones are areas created in the Automower Connect app that the mower avoids when enabled. " +
        "Returns each zone's UUID, name, and enabled status. " +
        "Not available on EPOS mowers. " +
        "Check the 'dirty' flag: if true, the map is out of sync and zones cannot be toggled.")]
    public Task<string> GetStayOutZones(
        [Description("UUID of the mower")] string mowerId,
        CancellationToken ct)
        => api.GetAsync($"mowers/{mowerId}/stayOutZones", ct);

    [McpServerTool]
    [Description(
        "Get a summary list of all work areas defined for a mower. " +
        "Work areas are named zones of the lawn that can be scheduled independently with their own cutting height. " +
        "Returns workAreaId, name, cuttingHeight, enabled status, and progress for each area.")]
    public Task<string> GetWorkAreas(
        [Description("UUID of the mower")] string mowerId,
        CancellationToken ct)
        => api.GetAsync($"mowers/{mowerId}/workAreas", ct);

    [McpServerTool]
    [Description(
        "Get detailed data for a specific work area including its calendar schedule and all settings. " +
        "Use GetWorkAreas first to discover available workAreaIds.")]
    public Task<string> GetWorkArea(
        [Description("UUID of the mower")] string mowerId,
        [Description("Numeric ID of the work area (e.g. 123456; use 0 for the default area)")] long workAreaId,
        CancellationToken ct)
        => api.GetAsync($"mowers/{mowerId}/workAreas/{workAreaId}", ct);
}
