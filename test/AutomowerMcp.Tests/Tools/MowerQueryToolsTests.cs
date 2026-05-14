using AutomowerMcp.Services;
using AutomowerMcp.McpTools;
using NSubstitute;
using Xunit;

namespace AutomowerMcp.Tests.Tools;

public class MowerQueryToolsTests
{
    private const string MowerId = "256b2365-33a7-46fe-a9fb-e67e84f4ac01";

    private readonly IAutomowerApiService _api;
    private readonly MowerQueryTools _sut;

    public MowerQueryToolsTests()
    {
        _api = Substitute.For<IAutomowerApiService>();
        _sut = new MowerQueryTools(_api);
    }

    [Fact]
    public async Task ListMowers_CallsGetMowersEndpoint_AndReturnsResponse()
    {
        const string expected = "[{\"id\":\"1\"}]";
        _api.GetAsync("mowers", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.ListMowers(CancellationToken.None);

        await _api.Received(1).GetAsync("mowers", Arg.Any<CancellationToken>());
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetMower_CallsEndpointWithMowerId()
    {
        _api.GetAsync($"mowers/{MowerId}", Arg.Any<CancellationToken>()).Returns("{\"data\":{}}");

        var result = await _sut.GetMower(MowerId, CancellationToken.None);

        await _api.Received(1).GetAsync($"mowers/{MowerId}", Arg.Any<CancellationToken>());
        Assert.Equal("{\"data\":{}}", result);
    }

    [Fact]
    public async Task GetMowerMessages_CallsCorrectEndpoint()
    {
        _api.GetAsync($"mowers/{MowerId}/messages", Arg.Any<CancellationToken>()).Returns("{\"data\":{}}");

        await _sut.GetMowerMessages(MowerId, CancellationToken.None);

        await _api.Received(1).GetAsync($"mowers/{MowerId}/messages", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStayOutZones_CallsCorrectEndpoint()
    {
        _api.GetAsync($"mowers/{MowerId}/stayOutZones", Arg.Any<CancellationToken>()).Returns("{\"data\":{}}");

        await _sut.GetStayOutZones(MowerId, CancellationToken.None);

        await _api.Received(1).GetAsync($"mowers/{MowerId}/stayOutZones", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetWorkAreas_CallsCorrectEndpoint()
    {
        _api.GetAsync($"mowers/{MowerId}/workAreas", Arg.Any<CancellationToken>()).Returns("{\"data\":[]}");

        await _sut.GetWorkAreas(MowerId, CancellationToken.None);

        await _api.Received(1).GetAsync($"mowers/{MowerId}/workAreas", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetWorkArea_CallsCorrectEndpointWithWorkAreaId()
    {
        const long workAreaId = 456789;
        _api.GetAsync($"mowers/{MowerId}/workAreas/{workAreaId}", Arg.Any<CancellationToken>()).Returns("{\"data\":{}}");

        var result = await _sut.GetWorkArea(MowerId, workAreaId, CancellationToken.None);

        await _api.Received(1).GetAsync($"mowers/{MowerId}/workAreas/{workAreaId}", Arg.Any<CancellationToken>());
        Assert.Equal("{\"data\":{}}", result);
    }
}
