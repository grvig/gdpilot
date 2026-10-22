using System.Text.Json;
using System.Text.Json.Nodes;

namespace GDPilot.Core;

/// <summary>
/// Writes a calibrated grid into the configuration's text, leaving everything
/// else as it was. The file is parsed and written back, so comments in it do
/// not survive a save; settings, bindings and other grids all do.
/// </summary>
public static class ConfigEditor
{
    private static readonly JsonDocumentOptions Reading = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly JsonSerializerOptions Writing = new() { WriteIndented = true };

    /// <summary>
    /// The configuration with the grid stored under the profile for this display,
    /// creating that profile if needed and making it the active one, since a grid
    /// just calibrated is the one the person means to use.
    /// </summary>
    public static string WithGrid(string json, LiveDisplay display, string gridName, GridSpec grid)
    {
        RequireModeName(gridName);
        JsonObject root = JsonNode.Parse(json, documentOptions: Reading)?.AsObject() ?? new JsonObject();

        JsonObject profiles = Child(root, "profiles");
        JsonObject? profile = profiles[display.ProfileKey] as JsonObject;

        if (profile is null)
        {
            profile = new JsonObject();
            profile["resolution"] = new JsonArray(display.Width, display.Height);
            profile["uiScale"] = display.UiScale;
            profiles[display.ProfileKey] = profile;
        }

        JsonObject grids = Child(profile, "grids");
        JsonObject entry = new();
        entry["originX"] = Math.Round(grid.OriginX, 1);
        entry["originY"] = Math.Round(grid.OriginY, 1);
        entry["pitchX"] = Math.Round(grid.PitchX, 2);
        entry["pitchY"] = Math.Round(grid.PitchY, 2);
        entry["columns"] = grid.Columns;
        entry["rows"] = grid.Rows;
        grids[gridName] = entry;

        root["activeProfile"] = display.ProfileKey;
        return root.ToJsonString(Writing);
    }

    private static JsonObject Child(JsonObject parent, string name)
    {
        if (parent[name] is JsonObject existing)
        {
            return existing;
        }

        JsonObject created = new();
        parent[name] = created;
        return created;
    }

    /// <summary>Grids are found by mode name, so a grid named after no mode could never be used.</summary>
    private static void RequireModeName(string gridName)
    {
        string mode = gridName.Split('.', 2)[0];

        if (!Enum.TryParse(mode, false, out Mode parsed) || parsed is Mode.Combat or Mode.Suspended || char.IsDigit(mode[0]))
        {
            throw new ArgumentException($"\"{gridName}\" does not start with a panel mode such as Inventory, Stash or Vendor.", nameof(gridName));
        }
    }
}
