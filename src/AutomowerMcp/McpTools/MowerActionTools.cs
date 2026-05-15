using AutomowerMcp.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace AutomowerMcp.McpTools;

/// <summary>
/// MCP tools that map to the write (POST/PATCH) endpoints of the Automower Connect API.
/// </summary>
[McpServerToolType]
public class MowerActionTools(IAutomowerApiService api)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    // -----------------------------------------------------------------------
    // Mowing actions  (/mowers/{id}/actions)
    // -----------------------------------------------------------------------

    [McpServerTool]
    [Description(
        "Start the mower and have it mow for a fixed duration, then return to the charging station. " +
        "The mower must be in a state that allows remote start (connected, no fatal errors). " +
        "Mode becomes SECONDARY_AREA for the duration.")]
    public Task<string> StartMowing(
        [Description("UUID of the mower")] string mowerId,
        [Description("How long to mow in minutes (1–65000)")] int durationMinutes,
        CancellationToken ct)
    {
        var body = new { data = new { type = "Start", attributes = new { duration = durationMinutes } } };
        return api.PostAsync($"mowers/{mowerId}/actions", body, ct);
    }

    [McpServerTool]
    [Description(
        "Start the mower in a specific work area. " +
        "Optionally supply a duration in minutes; pass 0 to let the mower continue until its battery runs low or the schedule ends. " +
        "Use GetWorkAreas to find valid workAreaIds.")]
    public Task<string> StartMowingInWorkArea(
        [Description("UUID of the mower")] string mowerId,
        [Description("Numeric ID of the work area to mow")] long workAreaId,
        [Description("Duration in minutes (0 = indefinite until battery or schedule ends)")] int durationMinutes,
        CancellationToken ct)
    {
        object attributes = durationMinutes > 0
            ? (object)new { duration = durationMinutes, workAreaId }
            : new { workAreaId };

        var body = new { data = new { type = "StartInWorkArea", attributes } };
        return api.PostAsync($"mowers/{mowerId}/actions", body, ct);
    }

    [McpServerTool]
    [Description(
        "Pause the mower at its current position. " +
        "The mower stops in the garden and waits. Use ResumeSchedule or StartMowing to continue.")]
    public Task<string> PauseMower(
        [Description("UUID of the mower")] string mowerId,
        CancellationToken ct)
    {
        var body = new { data = new { type = "Pause" } };
        return api.PostAsync($"mowers/{mowerId}/actions", body, ct);
    }

    [McpServerTool]
    [Description(
        "Resume the mower's calendar schedule. " +
        "Removes any active override (forced park or forced mow) so the mower follows its programmed week schedule again.")]
    public Task<string> ResumeSchedule(
        [Description("UUID of the mower")] string mowerId,
        CancellationToken ct)
    {
        var body = new { data = new { type = "ResumeSchedule" } };
        return api.PostAsync($"mowers/{mowerId}/actions", body, ct);
    }

    [McpServerTool]
    [Description(
        "Drive the mower to the charging station and park it for a fixed duration. " +
        "After the duration expires the mower automatically resumes its schedule. " +
        "Optionally supply an externalReason code (200000–299999) to identify which external integration triggered the parking " +
        "(useful when multiple integrations share the same API). Pass 0 for no external reason.")]
    public Task<string> ParkMower(
        [Description("UUID of the mower")] string mowerId,
        [Description("How long to park in minutes (1–65000, max 1500 when using externalReason)")] int durationMinutes,
        [Description("External reason code 200000–299999 (Developer Portal range); 0 to omit")] long externalReason,
        CancellationToken ct)
    {
        object attributes = externalReason > 0
            ? (object)new { duration = durationMinutes, externalReason }
            : new { duration = durationMinutes };

        var body = new { data = new { type = "Park", attributes } };
        return api.PostAsync($"mowers/{mowerId}/actions", body, ct);
    }

    [McpServerTool]
    [Description(
        "Park the mower until its next scheduled mowing window. " +
        "The mower drives home and waits; mowing resumes at the start of the next calendar task.")]
    public Task<string> ParkUntilNextSchedule(
        [Description("UUID of the mower")] string mowerId,
        CancellationToken ct)
    {
        var body = new { data = new { type = "ParkUntilNextSchedule" } };
        return api.PostAsync($"mowers/{mowerId}/actions", body, ct);
    }

    [McpServerTool]
    [Description(
        "Park the mower indefinitely until manually restarted. " +
        "The mower drives home and stays parked; mode becomes HOME. " +
        "A manual start or StartMowing call is required to resume mowing.")]
    public Task<string> ParkUntilFurtherNotice(
        [Description("UUID of the mower")] string mowerId,
        CancellationToken ct)
    {
        var body = new { data = new { type = "ParkUntilFurtherNotice" } };
        return api.PostAsync($"mowers/{mowerId}/actions", body, ct);
    }

    // -----------------------------------------------------------------------
    // Calendar  (/mowers/{id}/calendar)
    // -----------------------------------------------------------------------

    [McpServerTool]
    [Description(
        "Replace the mower's entire weekly mowing schedule with a new set of tasks. " +
        "All existing tasks are overwritten — include every task (new and existing) you want active. " +
        "Each task needs: start (minutes after midnight, 0–1439), duration (minutes, 1–1440), " +
        "and a boolean for each day: monday, tuesday, wednesday, thursday, friday, saturday, sunday. " +
        "Example tasksJson: [{\"start\":420,\"duration\":480,\"monday\":true,\"tuesday\":true,\"wednesday\":true,\"thursday\":true,\"friday\":true,\"saturday\":false,\"sunday\":false}]")]
    public Task<string> UpdateCalendar(
        [Description("UUID of the mower")] string mowerId,
        [Description("JSON array of CalendarTask objects")] string tasksJson,
        CancellationToken ct)
    {
        var tasks = JsonSerializer.Deserialize<JsonElement>(tasksJson);
        var body = new { data = new { type = "calendar", attributes = new { tasks } } };
        return api.PostAsync($"mowers/{mowerId}/calendar", body, ct);
    }

    // -----------------------------------------------------------------------
    // Error confirm  (/mowers/{id}/errors/confirm)
    // -----------------------------------------------------------------------

    [McpServerTool]
    [Description(
        "Confirm (dismiss) the current non-fatal confirmable error on the mower — same action as tapping 'OK' in the Automower Connect app. " +
        "Only succeeds when the mower's isErrorConfirmable flag is true. " +
        "Supported on: 405X, 415X, 435X AWD, 535 AWD, Ceora, EPOS and NERA models.")]
    public Task<string> ConfirmError(
        [Description("UUID of the mower")] string mowerId,
        CancellationToken ct)
        => api.PostAsync($"mowers/{mowerId}/errors/confirm", null, ct);

    // -----------------------------------------------------------------------
    // Settings  (/mowers/{id}/settings)
    // -----------------------------------------------------------------------

    [McpServerTool]
    [Description(
        "Update one or more mower settings in a single call. Pass null / 0 for any setting you do not want to change. " +
        "cuttingHeight: blade height 1–9 (null to skip). " +
        "headlightMode: ALWAYS_ON | ALWAYS_OFF | EVENING_ONLY | EVENING_AND_NIGHT (null to skip). " +
        "dateTime: Unix timestamp in seconds for clock sync (0 to skip). " +
        "timeZone: IANA tz name e.g. Europe/Stockholm (used with dateTime; not needed for work-area-capable mowers).")]
    public Task<string> UpdateSettings(
        [Description("UUID of the mower")] string mowerId,
        [Description("Cutting height 1–9, or 0 to leave unchanged")] int cuttingHeight,
        [Description("Headlight mode string, or empty string to leave unchanged")] string? headlightMode,
        [Description("Unix timestamp seconds for clock sync, or 0 to skip")] long dateTime,
        [Description("IANA time zone (e.g. Europe/Stockholm), or empty to skip")] string? timeZone,
        CancellationToken ct)
    {
        var attributes = new Dictionary<string, object?>();

        if (cuttingHeight is >= 1 and <= 9)
            attributes["cuttingHeight"] = cuttingHeight;

        if (!string.IsNullOrWhiteSpace(headlightMode))
            attributes["headlight"] = new { mode = headlightMode };

        if (dateTime > 0)
        {
            var timer = new Dictionary<string, object?> { ["dateTime"] = dateTime };
            if (!string.IsNullOrWhiteSpace(timeZone))
                timer["timeZone"] = timeZone;
            attributes["timer"] = timer;
        }

        var body = new { data = new { type = "settings", attributes } };
        return api.PostAsync($"mowers/{mowerId}/settings", body, ct);
    }

    // -----------------------------------------------------------------------
    // Statistics  (/mowers/{id}/statistics/resetCuttingBladeUsageTime)
    // -----------------------------------------------------------------------

    [McpServerTool]
    [Description(
        "Reset the cutting blade usage time counter to zero. " +
        "Run this after replacing the cutting blades so the statistics.cuttingBladeUsageTime counter " +
        "accurately reflects time since the last blade change.")]
    public Task<string> ResetCuttingBladeUsageTime(
        [Description("UUID of the mower")] string mowerId,
        CancellationToken ct)
        => api.PostAsync($"mowers/{mowerId}/statistics/resetCuttingBladeUsageTime", null, ct);

    // -----------------------------------------------------------------------
    // Stay-out zones  (/mowers/{id}/stayOutZones/{stayOutId})
    // -----------------------------------------------------------------------

    [McpServerTool]
    [Description(
        "Enable or disable a stay-out zone. " +
        "When enabled the mower will not enter the zone. " +
        "The operation will fail if the zone map is dirty (out of sync with the cloud). " +
        "Use GetStayOutZones to find zone UUIDs and check the dirty flag.")]
    public Task<string> UpdateStayOutZone(
        [Description("UUID of the mower")] string mowerId,
        [Description("UUID of the stay-out zone")] string stayOutZoneId,
        [Description("true to activate the zone (mower avoids it), false to deactivate")] bool enable,
        CancellationToken ct)
    {
        var body = new
        {
            data = new
            {
                id = stayOutZoneId,
                type = "stayOutZone",
                attributes = new { enable }
            }
        };
        return api.PatchAsync($"mowers/{mowerId}/stayOutZones/{stayOutZoneId}", body, ct);
    }

    // -----------------------------------------------------------------------
    // Work areas  (/mowers/{id}/workAreas/{workAreaId})
    // -----------------------------------------------------------------------

    [McpServerTool]
    [Description(
        "Update properties of a work area. Pass null / -1 for fields you do not want to change. " +
        "cuttingHeight: 0–100 % (-1 to skip). " +
        "enable: enable/disable the work area (null to skip). " +
        "name: display name max 32 chars (null/empty to skip). " +
        "orientation: base line angle 0–1800 for systematic mowing (-1 to skip, EPOS only). " +
        "orientationShift: pattern type — 0 parallel, 600 triangle, 900 square (−1 to skip, EPOS only).")]
    public Task<string> UpdateWorkArea(
        [Description("UUID of the mower")] string mowerId,
        [Description("Numeric ID of the work area")] long workAreaId,
        [Description("Cutting height percent 0–100, or -1 to skip")] int cuttingHeight,
        [Description("Enable or disable the area, or null to leave unchanged")] bool? enable,
        [Description("New name for the area (max 32 chars), or null/empty to leave unchanged")] string? name,
        [Description("Systematic mowing base orientation 0–1800, or -1 to skip")] int orientation,
        [Description("Pattern shift 0/600/900 for parallel/triangle/square, or -1 to skip")] int orientationShift,
        CancellationToken ct)
    {
        var attributes = new Dictionary<string, object?>();
        if (cuttingHeight is >= 0 and <= 100) attributes["cuttingHeight"] = cuttingHeight;
        if (enable.HasValue) attributes["enable"] = enable.Value;
        if (!string.IsNullOrWhiteSpace(name)) attributes["name"] = name;
        if (orientation is >= 0 and <= 1800) attributes["orientation"] = orientation;
        if (orientationShift is >= 0 and <= 1800) attributes["orientationShift"] = orientationShift;

        var body = new { data = new { id = workAreaId, type = "workArea", attributes } };
        return api.PatchAsync($"mowers/{mowerId}/workAreas/{workAreaId}", body, ct);
    }

    [McpServerTool]
    [Description(
        "Replace the calendar schedule for a single work area. All existing tasks for this work area are overwritten. " +
        "Each task must include workAreaId plus the standard schedule fields: " +
        "start (minutes after midnight), duration (minutes), and a boolean per day (monday–sunday). " +
        "Example tasksJson: [{\"start\":420,\"duration\":480,\"monday\":true,\"tuesday\":true,\"wednesday\":true,\"thursday\":true,\"friday\":true,\"saturday\":false,\"sunday\":false,\"workAreaId\":123456}]")]
    public Task<string> UpdateWorkAreaCalendar(
        [Description("UUID of the mower")] string mowerId,
        [Description("Numeric ID of the work area")] long workAreaId,
        [Description("JSON array of CalendarTaskWorkArea objects (each must include workAreaId)")] string tasksJson,
        CancellationToken ct)
    {
        var tasks = JsonSerializer.Deserialize<JsonElement>(tasksJson);
        var body = new { data = new { type = "calendar", attributes = new { tasks } } };
        return api.PostAsync($"mowers/{mowerId}/workAreas/{workAreaId}/calendar", body, ct);
    }
}
