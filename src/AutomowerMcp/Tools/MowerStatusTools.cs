using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace AutomowerMcp.Tools;

[McpServerToolType]
public static class MowerStatusTools
{
    [McpServerTool]
    [Description(
        "Get descriptions for all possible values of the mower status fields: mode, activity, and state. " +
        "These descriptions are already inlined into every GetMower and ListMowers response as modeDescription, " +
        "activityDescription, and stateDescription fields. Use this tool only if you encounter an unfamiliar value.")]
    public static string GetStatusDescriptions()
    {
        return JsonSerializer.Serialize(new
        {
            mode = AutomowerCodes.ModeDescriptions,
            activity = AutomowerCodes.ActivityDescriptions,
            state = AutomowerCodes.StateDescriptions,
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}
