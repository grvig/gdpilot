using System.Text.Json;
using System.Text.Json.Nodes;

namespace GDPilot.Core;

/// <summary>
/// Something the in-game part tells GDPilot. Each report is one line of JSON,
/// so either side can be read and checked by eye, and a line the other side
/// does not know is skipped rather than breaking the connection.
/// </summary>
public abstract record GameReport
{
    /// <summary>Raised when the two parts change how they talk, so a mismatched pair says so instead of misreading.</summary>
    public const int Protocol = 1;

    private static readonly JsonSerializerOptions Writing = new() { WriteIndented = false };

    /// <summary>The report on one line, or null for a line of a kind this version does not know.</summary>
    public static GameReport? Parse(string line)
    {
        JsonObject root;

        try
        {
            root = JsonNode.Parse(line)?.AsObject() ?? throw new FormatException("An empty report.");
        }
        catch (JsonException problem)
        {
            throw new FormatException($"A report that is not JSON: {problem.Message}");
        }

        int protocol = root["protocol"]?.GetValue<int>() ?? 0;

        if (protocol != Protocol)
        {
            throw new FormatException($"The in-game part speaks protocol {protocol}, but this GDPilot speaks {Protocol}. Update both from the same release.");
        }

        switch (root["type"]?.GetValue<string>())
        {
            case "panels":
                return new PanelsReport(Strings(root["open"]));
            case "layout":
                return Layout(root);
            case "camera":
                return new CameraReport(root["third"]?.GetValue<bool>() ?? false);
            case "health":
                return new HealthReport(Math.Clamp(root["percent"]?.GetValue<int>() ?? 100, 0, 100));
            case "targets":
                return Targets(root);
            default:
                return null;
        }
    }

    public string Write()
    {
        JsonObject root = new() { ["protocol"] = Protocol };

        switch (this)
        {
            case PanelsReport panels:
                root["type"] = "panels";
                root["open"] = new JsonArray(panels.Open.Select(name => (JsonNode?)JsonValue.Create(name)).ToArray());
                break;
            case LayoutReport layout:
                root["type"] = "layout";
                root["grid"] = layout.Grid;
                root["columns"] = layout.Columns;
                root["rows"] = layout.Rows;
                root["items"] = new JsonArray(layout.Items.Select(Item).ToArray());
                break;
            case CameraReport camera:
                root["type"] = "camera";
                root["third"] = camera.ThirdPerson;
                break;
            case HealthReport health:
                root["type"] = "health";
                root["percent"] = health.Percent;
                break;
            case TargetsReport targets:
                root["type"] = "targets";
                root["list"] = new JsonArray(targets.Targets.Select(Target).ToArray());
                break;
        }

        return root.ToJsonString(Writing);
    }

    private static LayoutReport Layout(JsonObject root)
    {
        string grid = root["grid"]?.GetValue<string>() ?? throw new FormatException("A layout report needs the grid it describes.");
        int columns = root["columns"]?.GetValue<int>() ?? 0;
        int rows = root["rows"]?.GetValue<int>() ?? 0;

        if (columns <= 0 || rows <= 0)
        {
            throw new FormatException($"The layout of {grid} needs a positive size.");
        }

        List<ItemRect> items = new();

        foreach (JsonNode? node in root["items"]?.AsArray() ?? new JsonArray())
        {
            JsonObject item = node?.AsObject() ?? throw new FormatException($"An empty item in the layout of {grid}.");
            ItemRect rect = new(Number(item, "id"), Number(item, "column"), Number(item, "row"), Number(item, "width"), Number(item, "height"));

            if (rect.Id == 0 || rect.Width <= 0 || rect.Height <= 0)
            {
                throw new FormatException($"An item in the layout of {grid} needs a non-zero id and a positive size.");
            }

            items.Add(rect);
        }

        return new LayoutReport(grid, columns, rows, items);
    }

    private static TargetsReport Targets(JsonObject root)
    {
        List<SeenTarget> targets = new();

        foreach (JsonNode? node in root["list"]?.AsArray() ?? new JsonArray())
        {
            JsonObject target = node?.AsObject() ?? throw new FormatException("An empty target.");
            uint id = target["id"]?.GetValue<uint>() ?? 0;

            if (id == 0)
            {
                throw new FormatException("A target needs a non-zero id.");
            }

            double x = target["x"]?.GetValue<double>() ?? throw new FormatException($"Target {id} has no position.");
            double y = target["y"]?.GetValue<double>() ?? throw new FormatException($"Target {id} has no position.");
            double distance = target["distance"]?.GetValue<double>() ?? 0;
            int health = Math.Clamp(target["health"]?.GetValue<int>() ?? 100, 0, 100);
            targets.Add(new SeenTarget(id, x, y, distance, health, target["seen"]?.GetValue<bool>() ?? false));
        }

        return new TargetsReport(targets);
    }

    private static JsonNode Target(SeenTarget target)
    {
        return new JsonObject
        {
            ["id"] = target.Id,
            ["x"] = target.X,
            ["y"] = target.Y,
            ["distance"] = target.Distance,
            ["health"] = target.Health,
            ["seen"] = target.OnScreen,
        };
    }

    private static JsonNode Item(ItemRect item)
    {
        return new JsonObject
        {
            ["id"] = item.Id,
            ["column"] = item.Column,
            ["row"] = item.Row,
            ["width"] = item.Width,
            ["height"] = item.Height,
        };
    }

    private static int Number(JsonObject item, string name)
    {
        return item[name]?.GetValue<int>() ?? throw new FormatException($"An item is missing its {name}.");
    }

    private static List<string> Strings(JsonNode? node)
    {
        List<string> names = new();

        foreach (JsonNode? entry in node?.AsArray() ?? new JsonArray())
        {
            names.Add(entry?.GetValue<string>() ?? throw new FormatException("An empty panel name."));
        }

        return names;
    }
}

/// <summary>Which panels the game has open right now, by mode name, such as "Inventory" or "Stash".</summary>
public sealed record PanelsReport(IReadOnlyList<string> Open) : GameReport;

/// <summary>The player's life as a whole percentage of its limit, sent whenever it moves.</summary>
public sealed record HealthReport(int Percent) : GameReport;

/// <summary>Where every item sits in one grid, named as in the profiles, such as "Inventory.Bag2".</summary>
public sealed record LayoutReport(string Grid, int Columns, int Rows, IReadOnlyList<ItemRect> Items) : GameReport;

/// <summary>
/// One enemy near the player, as lock-on sees it: where the game draws it, as
/// fractions of the game window from the top left, how far it stands from the
/// player in world units, its life in percent, and whether it is in view.
/// </summary>
public sealed record SeenTarget(uint Id, double X, double Y, double Distance, int Health, bool OnScreen);

/// <summary>Every enemy the game would let a skill hit within reach of the player, sent whenever one moves.</summary>
public sealed record TargetsReport(IReadOnlyList<SeenTarget> Targets) : GameReport;
