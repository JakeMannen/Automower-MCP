using AutomowerMcp;
using System.Text.Json;
using Xunit;

namespace AutomowerMcp.Tests;

public class ResponseEnricherTests
{
    [Fact]
    public void Enrich_AddsErrorCodeDescription_ForKnownCode()
    {
        var json = """{"errorCode":31}""";
        var result = JsonDocument.Parse(ResponseEnricher.Enrich(json)).RootElement;
        Assert.Equal("STOP button problem", result.GetProperty("errorCodeDescription").GetString());
    }

    [Fact]
    public void Enrich_AddsErrorCodeDescription_ForZero()
    {
        var json = """{"errorCode":0}""";
        var result = JsonDocument.Parse(ResponseEnricher.Enrich(json)).RootElement;
        Assert.Equal("No message", result.GetProperty("errorCodeDescription").GetString());
    }

    [Fact]
    public void Enrich_DoesNotAddDescription_ForUnknownCode()
    {
        var json = """{"errorCode":99999}""";
        var result = JsonDocument.Parse(ResponseEnricher.Enrich(json)).RootElement;
        Assert.False(result.TryGetProperty("errorCodeDescription", out _));
    }

    [Fact]
    public void Enrich_AddsModeDescription()
    {
        var json = """{"mode":"MAIN_AREA"}""";
        var result = JsonDocument.Parse(ResponseEnricher.Enrich(json)).RootElement;
        Assert.True(result.TryGetProperty("modeDescription", out var desc));
        Assert.False(string.IsNullOrEmpty(desc.GetString()));
    }

    [Fact]
    public void Enrich_AddsActivityDescription()
    {
        var json = """{"activity":"MOWING"}""";
        var result = JsonDocument.Parse(ResponseEnricher.Enrich(json)).RootElement;
        Assert.True(result.TryGetProperty("activityDescription", out var desc));
        Assert.False(string.IsNullOrEmpty(desc.GetString()));
    }

    [Fact]
    public void Enrich_AddsStateDescription()
    {
        var json = """{"state":"IN_OPERATION"}""";
        var result = JsonDocument.Parse(ResponseEnricher.Enrich(json)).RootElement;
        Assert.True(result.TryGetProperty("stateDescription", out var desc));
        Assert.False(string.IsNullOrEmpty(desc.GetString()));
    }

    [Fact]
    public void Enrich_EnrichesNestedObjects()
    {
        var json = """{"data":{"attributes":{"mower":{"errorCode":31,"mode":"MAIN_AREA","activity":"MOWING","state":"IN_OPERATION"}}}}""";
        var mower = JsonDocument.Parse(ResponseEnricher.Enrich(json))
            .RootElement
            .GetProperty("data")
            .GetProperty("attributes")
            .GetProperty("mower");

        Assert.Equal("STOP button problem", mower.GetProperty("errorCodeDescription").GetString());
        Assert.True(mower.TryGetProperty("modeDescription", out _));
        Assert.True(mower.TryGetProperty("activityDescription", out _));
        Assert.True(mower.TryGetProperty("stateDescription", out _));
    }

    [Fact]
    public void Enrich_EnrichesObjectsInsideArrays()
    {
        var json = """{"messages":[{"errorCode":31},{"errorCode":0}]}""";
        var messages = JsonDocument.Parse(ResponseEnricher.Enrich(json))
            .RootElement
            .GetProperty("messages");

        Assert.Equal("STOP button problem", messages[0].GetProperty("errorCodeDescription").GetString());
        Assert.Equal("No message", messages[1].GetProperty("errorCodeDescription").GetString());
    }

    [Fact]
    public void Enrich_ReturnsSameString_WhenNoMatchingFields()
    {
        var json = """{"id":"abc","name":"My Mower"}""";
        var result = JsonDocument.Parse(ResponseEnricher.Enrich(json)).RootElement;
        Assert.Equal("abc", result.GetProperty("id").GetString());
        Assert.Equal("My Mower", result.GetProperty("name").GetString());
        Assert.Equal(2, result.EnumerateObject().Count());
    }

    [Fact]
    public void Enrich_ReturnsOriginal_WhenInputIsNotJson()
    {
        const string notJson = "{\"result\":\"success\"}";
        Assert.Equal(notJson, ResponseEnricher.Enrich(notJson));
    }

    [Fact]
    public void Enrich_DoesNotAddDescription_ForUnknownMode()
    {
        var json = """{"mode":"FUTURE_VALUE"}""";
        var result = JsonDocument.Parse(ResponseEnricher.Enrich(json)).RootElement;
        Assert.False(result.TryGetProperty("modeDescription", out _));
    }
}
