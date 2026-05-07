using System.Text.Json;
using System.Text.Json.Nodes;

namespace AutomowerMcp;

/// <summary>
/// Enriches Husqvarna API JSON responses by injecting human-readable descriptions
/// alongside opaque numeric error codes and status enum values (mode, activity, state).
/// </summary>
internal static class ResponseEnricher
{
    internal static string Enrich(string json)
    {
        JsonNode? root;
        try { root = JsonNode.Parse(json); }
        catch (JsonException) { return json; }

        if (root is null) return json;

        EnrichNode(root);
        return root.ToJsonString();
    }

    private static void EnrichNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            // Snapshot keys before mutating so the foreach below is stable
            var keys = obj.Select(kvp => kvp.Key).ToList();

            if (obj["errorCode"] is JsonValue errorCodeVal &&
                errorCodeVal.TryGetValue<int>(out var code) &&
                AutomowerCodes.ErrorCodes.TryGetValue(code, out var errorDesc))
            {
                obj["errorCodeDescription"] = errorDesc;
            }

            EnrichEnum(obj, "mode",     AutomowerCodes.ModeDescriptions);
            EnrichEnum(obj, "activity", AutomowerCodes.ActivityDescriptions);
            EnrichEnum(obj, "state",    AutomowerCodes.StateDescriptions);

            // Recurse only into original children; added description fields are leaf strings
            foreach (var key in keys)
            {
                if (obj[key] is JsonNode child)
                    EnrichNode(child);
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item is not null)
                    EnrichNode(item);
            }
        }
    }

    private static void EnrichEnum(JsonObject obj, string key, IReadOnlyDictionary<string, string> lookup)
    {
        if (obj[key] is JsonValue val &&
            val.TryGetValue<string>(out var str) &&
            lookup.TryGetValue(str, out var desc))
        {
            obj[$"{key}Description"] = desc;
        }
    }
}
