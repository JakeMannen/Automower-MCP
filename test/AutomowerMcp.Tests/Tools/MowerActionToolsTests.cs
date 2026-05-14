using AutomowerMcp.Services;
using AutomowerMcp.McpTools;
using NSubstitute;
using NSubstitute.Core;
using System.Text.Json;
using Xunit;

namespace AutomowerMcp.Tests.Tools;

public class MowerActionToolsTests
{
    private const string MowerId = "256b2365-33a7-46fe-a9fb-e67e84f4ac01";

    private readonly IAutomowerApiService _api;
    private readonly MowerActionTools _sut;

    public MowerActionToolsTests()
    {
        _api = Substitute.For<IAutomowerApiService>();
        _sut = new MowerActionTools(_api);
        _api.PostAsync(Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns("{\"data\":{}}");
        _api.PatchAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns("{\"data\":{}}");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static JsonElement ParseBody(ICall call)
    {
        var json = JsonSerializer.Serialize(call.GetArguments()[1]);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private ICall GetLastPost(string path)
        => _api.ReceivedCalls()
            .Last(c => c.GetMethodInfo().Name == "PostAsync" &&
                       c.GetArguments()[0] as string == path);

    private ICall GetLastPatch(string path)
        => _api.ReceivedCalls()
            .Last(c => c.GetMethodInfo().Name == "PatchAsync" &&
                       c.GetArguments()[0] as string == path);

    // ── StartMowing ────────────────────────────────────────────────────────

    [Fact]
    public async Task StartMowing_PostsStartTypeWithDuration()
    {
        await _sut.StartMowing(MowerId, 30, CancellationToken.None);

        await _api.Received(1).PostAsync(
            $"mowers/{MowerId}/actions", Arg.Any<object?>(), Arg.Any<CancellationToken>());
        var data = ParseBody(GetLastPost($"mowers/{MowerId}/actions")).GetProperty("data");
        Assert.Equal("Start", data.GetProperty("type").GetString());
        Assert.Equal(30, data.GetProperty("attributes").GetProperty("duration").GetInt32());
    }

    // ── StartMowingInWorkArea ──────────────────────────────────────────────

    [Fact]
    public async Task StartMowingInWorkArea_WithDuration_IncludesBothDurationAndWorkAreaId()
    {
        await _sut.StartMowingInWorkArea(MowerId, 12345, 60, CancellationToken.None);

        var data = ParseBody(GetLastPost($"mowers/{MowerId}/actions")).GetProperty("data");
        Assert.Equal("StartInWorkArea", data.GetProperty("type").GetString());
        var attrs = data.GetProperty("attributes");
        Assert.Equal(60, attrs.GetProperty("duration").GetInt32());
        Assert.Equal(12345, attrs.GetProperty("workAreaId").GetInt64());
    }

    [Fact]
    public async Task StartMowingInWorkArea_ZeroDuration_OmitsDuration()
    {
        await _sut.StartMowingInWorkArea(MowerId, 12345, 0, CancellationToken.None);

        var attrs = ParseBody(GetLastPost($"mowers/{MowerId}/actions"))
            .GetProperty("data").GetProperty("attributes");
        Assert.False(attrs.TryGetProperty("duration", out _));
        Assert.Equal(12345, attrs.GetProperty("workAreaId").GetInt64());
    }

    // ── PauseMower ─────────────────────────────────────────────────────────

    [Fact]
    public async Task PauseMower_PostsPauseType()
    {
        await _sut.PauseMower(MowerId, CancellationToken.None);

        var data = ParseBody(GetLastPost($"mowers/{MowerId}/actions")).GetProperty("data");
        Assert.Equal("Pause", data.GetProperty("type").GetString());
    }

    // ── ResumeSchedule ─────────────────────────────────────────────────────

    [Fact]
    public async Task ResumeSchedule_PostsResumeScheduleType()
    {
        await _sut.ResumeSchedule(MowerId, CancellationToken.None);

        var data = ParseBody(GetLastPost($"mowers/{MowerId}/actions")).GetProperty("data");
        Assert.Equal("ResumeSchedule", data.GetProperty("type").GetString());
    }

    // ── ParkMower ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ParkMower_WithoutExternalReason_PostsDurationOnly()
    {
        await _sut.ParkMower(MowerId, 120, 0, CancellationToken.None);

        var data = ParseBody(GetLastPost($"mowers/{MowerId}/actions")).GetProperty("data");
        Assert.Equal("Park", data.GetProperty("type").GetString());
        var attrs = data.GetProperty("attributes");
        Assert.Equal(120, attrs.GetProperty("duration").GetInt32());
        Assert.False(attrs.TryGetProperty("externalReason", out _));
    }

    [Fact]
    public async Task ParkMower_WithExternalReason_IncludesExternalReasonInBody()
    {
        await _sut.ParkMower(MowerId, 60, 200001, CancellationToken.None);

        var attrs = ParseBody(GetLastPost($"mowers/{MowerId}/actions"))
            .GetProperty("data").GetProperty("attributes");
        Assert.Equal(60, attrs.GetProperty("duration").GetInt32());
        Assert.Equal(200001, attrs.GetProperty("externalReason").GetInt64());
    }

    // ── ParkUntilNextSchedule ──────────────────────────────────────────────

    [Fact]
    public async Task ParkUntilNextSchedule_PostsCorrectType()
    {
        await _sut.ParkUntilNextSchedule(MowerId, CancellationToken.None);

        var data = ParseBody(GetLastPost($"mowers/{MowerId}/actions")).GetProperty("data");
        Assert.Equal("ParkUntilNextSchedule", data.GetProperty("type").GetString());
    }

    // ── ParkUntilFurtherNotice ─────────────────────────────────────────────

    [Fact]
    public async Task ParkUntilFurtherNotice_PostsCorrectType()
    {
        await _sut.ParkUntilFurtherNotice(MowerId, CancellationToken.None);

        var data = ParseBody(GetLastPost($"mowers/{MowerId}/actions")).GetProperty("data");
        Assert.Equal("ParkUntilFurtherNotice", data.GetProperty("type").GetString());
    }

    // ── UpdateCalendar ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCalendar_PostsToCalendarEndpointWithTasks()
    {
        const string tasksJson =
            "[{\"start\":420,\"duration\":480,\"monday\":true,\"tuesday\":false," +
            "\"wednesday\":true,\"thursday\":false,\"friday\":false,\"saturday\":false,\"sunday\":false}]";

        await _sut.UpdateCalendar(MowerId, tasksJson, CancellationToken.None);

        await _api.Received(1).PostAsync(
            $"mowers/{MowerId}/calendar", Arg.Any<object?>(), Arg.Any<CancellationToken>());
        var data = ParseBody(GetLastPost($"mowers/{MowerId}/calendar")).GetProperty("data");
        Assert.Equal("calendar", data.GetProperty("type").GetString());
        var tasks = data.GetProperty("attributes").GetProperty("tasks");
        Assert.Equal(JsonValueKind.Array, tasks.ValueKind);
        Assert.Equal(420, tasks[0].GetProperty("start").GetInt32());
        Assert.Equal(480, tasks[0].GetProperty("duration").GetInt32());
    }

    // ── ConfirmError ───────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmError_PostsToCorrectEndpointWithNullBody()
    {
        await _sut.ConfirmError(MowerId, CancellationToken.None);

        await _api.Received(1).PostAsync(
            $"mowers/{MowerId}/errors/confirm", null, Arg.Any<CancellationToken>());
    }

    // ── UpdateSettings ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSettings_ValidCuttingHeight_IncludesHeightAndOmitsRest()
    {
        await _sut.UpdateSettings(MowerId, 5, null, 0, null, CancellationToken.None);

        await _api.Received(1).PostAsync(
            $"mowers/{MowerId}/settings", Arg.Any<object?>(), Arg.Any<CancellationToken>());
        var attrs = ParseBody(GetLastPost($"mowers/{MowerId}/settings"))
            .GetProperty("data").GetProperty("attributes");
        Assert.Equal(5, attrs.GetProperty("cuttingHeight").GetInt32());
        Assert.False(attrs.TryGetProperty("headlight", out _));
        Assert.False(attrs.TryGetProperty("timer", out _));
    }

    [Fact]
    public async Task UpdateSettings_InvalidCuttingHeight_OmitsCuttingHeightFromBody()
    {
        await _sut.UpdateSettings(MowerId, 0, null, 0, null, CancellationToken.None);

        var attrs = ParseBody(GetLastPost($"mowers/{MowerId}/settings"))
            .GetProperty("data").GetProperty("attributes");
        Assert.False(attrs.TryGetProperty("cuttingHeight", out _));
    }

    [Fact]
    public async Task UpdateSettings_WithHeadlightMode_IncludesHeadlightObject()
    {
        await _sut.UpdateSettings(MowerId, 0, "ALWAYS_ON", 0, null, CancellationToken.None);

        var attrs = ParseBody(GetLastPost($"mowers/{MowerId}/settings"))
            .GetProperty("data").GetProperty("attributes");
        Assert.Equal("ALWAYS_ON", attrs.GetProperty("headlight").GetProperty("mode").GetString());
    }

    [Fact]
    public async Task UpdateSettings_WithDateTimeAndTimeZone_IncludesTimer()
    {
        await _sut.UpdateSettings(MowerId, 0, null, 1723449269L, "Europe/Stockholm", CancellationToken.None);

        var timer = ParseBody(GetLastPost($"mowers/{MowerId}/settings"))
            .GetProperty("data").GetProperty("attributes").GetProperty("timer");
        Assert.Equal(1723449269L, timer.GetProperty("dateTime").GetInt64());
        Assert.Equal("Europe/Stockholm", timer.GetProperty("timeZone").GetString());
    }

    [Fact]
    public async Task UpdateSettings_WithDateTimeNoTimeZone_OmitsTimeZoneFromTimer()
    {
        await _sut.UpdateSettings(MowerId, 0, null, 1723449269L, null, CancellationToken.None);

        var timer = ParseBody(GetLastPost($"mowers/{MowerId}/settings"))
            .GetProperty("data").GetProperty("attributes").GetProperty("timer");
        Assert.Equal(1723449269L, timer.GetProperty("dateTime").GetInt64());
        Assert.False(timer.TryGetProperty("timeZone", out _));
    }

    // ── ResetCuttingBladeUsageTime ─────────────────────────────────────────

    [Fact]
    public async Task ResetCuttingBladeUsageTime_PostsToCorrectEndpointWithNullBody()
    {
        await _sut.ResetCuttingBladeUsageTime(MowerId, CancellationToken.None);

        await _api.Received(1).PostAsync(
            $"mowers/{MowerId}/statistics/resetCuttingBladeUsageTime",
            null,
            Arg.Any<CancellationToken>());
    }

    // ── UpdateStayOutZone ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStayOutZone_PatchesCorrectEndpointWithEnableFlag()
    {
        const string zoneId = "81C6EEA2-D139-4FEA-B134-F22A6B3EA403";

        await _sut.UpdateStayOutZone(MowerId, zoneId, true, CancellationToken.None);

        await _api.Received(1).PatchAsync(
            $"mowers/{MowerId}/stayOutZones/{zoneId}", Arg.Any<object>(), Arg.Any<CancellationToken>());
        var data = ParseBody(GetLastPatch($"mowers/{MowerId}/stayOutZones/{zoneId}")).GetProperty("data");
        Assert.Equal("stayOutZone", data.GetProperty("type").GetString());
        Assert.Equal(zoneId, data.GetProperty("id").GetString());
        Assert.True(data.GetProperty("attributes").GetProperty("enable").GetBoolean());
    }

    [Fact]
    public async Task UpdateStayOutZone_Disable_SetEnableFalse()
    {
        const string zoneId = "81C6EEA2-D139-4FEA-B134-F22A6B3EA403";

        await _sut.UpdateStayOutZone(MowerId, zoneId, false, CancellationToken.None);

        var attrs = ParseBody(GetLastPatch($"mowers/{MowerId}/stayOutZones/{zoneId}"))
            .GetProperty("data").GetProperty("attributes");
        Assert.False(attrs.GetProperty("enable").GetBoolean());
    }

    // ── UpdateWorkArea ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateWorkArea_PatchesCorrectEndpointWithAllProvidedAttributes()
    {
        const long workAreaId = 12345;

        await _sut.UpdateWorkArea(MowerId, workAreaId, 50, true, "Front Lawn", -1, -1, CancellationToken.None);

        await _api.Received(1).PatchAsync(
            $"mowers/{MowerId}/workAreas/{workAreaId}", Arg.Any<object>(), Arg.Any<CancellationToken>());
        var data = ParseBody(GetLastPatch($"mowers/{MowerId}/workAreas/{workAreaId}")).GetProperty("data");
        Assert.Equal("workArea", data.GetProperty("type").GetString());
        Assert.Equal(workAreaId, data.GetProperty("id").GetInt64());
        var attrs = data.GetProperty("attributes");
        Assert.Equal(50, attrs.GetProperty("cuttingHeight").GetInt32());
        Assert.True(attrs.GetProperty("enable").GetBoolean());
        Assert.Equal("Front Lawn", attrs.GetProperty("name").GetString());
        Assert.False(attrs.TryGetProperty("orientation", out _));
        Assert.False(attrs.TryGetProperty("orientationShift", out _));
    }

    [Fact]
    public async Task UpdateWorkArea_NegativeCuttingHeight_OmitsCuttingHeight()
    {
        await _sut.UpdateWorkArea(MowerId, 1, -1, null, null, -1, -1, CancellationToken.None);

        var attrs = ParseBody(GetLastPatch($"mowers/{MowerId}/workAreas/1"))
            .GetProperty("data").GetProperty("attributes");
        Assert.False(attrs.TryGetProperty("cuttingHeight", out _));
    }

    [Fact]
    public async Task UpdateWorkArea_WithOrientation_IncludesOrientationFields()
    {
        await _sut.UpdateWorkArea(MowerId, 1, -1, null, null, 900, 600, CancellationToken.None);

        var attrs = ParseBody(GetLastPatch($"mowers/{MowerId}/workAreas/1"))
            .GetProperty("data").GetProperty("attributes");
        Assert.Equal(900, attrs.GetProperty("orientation").GetInt32());
        Assert.Equal(600, attrs.GetProperty("orientationShift").GetInt32());
    }

    // ── UpdateWorkAreaCalendar ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateWorkAreaCalendar_PostsToCorrectEndpointWithTasks()
    {
        const long workAreaId = 12345;
        const string tasksJson =
            "[{\"start\":300,\"duration\":360,\"monday\":false,\"tuesday\":true," +
            "\"wednesday\":false,\"thursday\":false,\"friday\":false,\"saturday\":false," +
            "\"sunday\":false,\"workAreaId\":12345}]";

        await _sut.UpdateWorkAreaCalendar(MowerId, workAreaId, tasksJson, CancellationToken.None);

        await _api.Received(1).PostAsync(
            $"mowers/{MowerId}/workAreas/{workAreaId}/calendar",
            Arg.Any<object?>(),
            Arg.Any<CancellationToken>());
        var data = ParseBody(GetLastPost($"mowers/{MowerId}/workAreas/{workAreaId}/calendar")).GetProperty("data");
        Assert.Equal("calendar", data.GetProperty("type").GetString());
        var tasks = data.GetProperty("attributes").GetProperty("tasks");
        Assert.Equal(JsonValueKind.Array, tasks.ValueKind);
        Assert.Equal(300, tasks[0].GetProperty("start").GetInt32());
        Assert.Equal(12345, tasks[0].GetProperty("workAreaId").GetInt64());
    }
}
