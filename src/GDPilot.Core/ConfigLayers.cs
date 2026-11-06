using System.Text.Json;
using System.Text.Json.Nodes;

namespace GDPilot.Core;

/// <summary>
/// A person's settings laid over the ones shipped, so that an update's new windows
/// and buttons reach them without losing what they changed. Their copy leaves out
/// the measured places, which they have no reason to edit and every reason to
/// have kept current.
/// </summary>
public static class ConfigLayers
{
    private const string Measurements = "profiles";

    private static readonly JsonDocumentOptions Lenient = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
    private static readonly JsonSerializerOptions Readable = new() { WriteIndented = true };

    /// <summary>The shipped settings with the person's on top: tables merge, and any other value of theirs wins.</summary>
    public static string Over(string shipped, string own)
    {
        JsonObject under = Parse(shipped);
        Lay(Parse(own), under);
        return under.ToJsonString();
    }

    /// <summary>What a person starts with: everything shipped but the measured places.</summary>
    public static string ForAPerson(string shipped)
    {
        JsonObject settings = Parse(shipped);
        settings.Remove(Measurements);
        return settings.ToJsonString(Readable);
    }

    private static JsonObject Parse(string text)
    {
        if (JsonNode.Parse(text, null, Lenient) is JsonObject parsed)
        {
            return parsed;
        }

        throw new FormatException("The settings are not a JSON object.");
    }

    private static void Lay(JsonObject top, JsonObject under)
    {
        foreach ((string name, JsonNode? value) in top.ToList())
        {
            if (value is JsonObject table && under[name] is JsonObject below)
            {
                Lay(table, below);
                continue;
            }

            top.Remove(name);
            under[name] = value;
        }
    }
}
