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
            case "interactables":
                return Interactables(root);
            case "abilities":
                return new AbilitiesReport(root["analog"]?.GetValue<bool>() ?? false);
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
            case InteractablesReport near:
                root["type"] = "interactables";
                root["list"] = new JsonArray(near.Things.Select(Thing).ToArray());
                break;
            case AbilitiesReport abilities:
                root["type"] = "abilities";
                root["analog"] = abilities.AnalogMovement;
                break;
            case TargetsReport targets:
                root["type"] = "targets";
                root["list"] = new JsonArray(targets.Targets.Select(Target).ToArray());

                if (targets.Feet is not null)
                {
                    root["feet"] = Spot(targets.Feet);
                    root["ring"] = new JsonArray(targets.Ring.Select(Spot).ToArray());
                }

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

        // Thirty of these arrive a second, so one enemy the game described badly is
        // passed over rather than costing the whole report, and with it the lock.
        foreach (JsonNode? node in root["list"]?.AsArray() ?? new JsonArray())
        {
            if (node is not JsonObject target || !Has(target, "id", out uint id) || id == 0 || !Has(target, "x", out double x) || !Has(target, "y", out double y))
            {
                continue;
            }

            Has(target, "distance", out double distance);
            int health = 100;

            if (Has(target, "health", out int reported))
            {
                health = Math.Clamp(reported, 0, 100);
            }

            Has(target, "seen", out bool seen);
            targets.Add(new SeenTarget(id, x, y, distance, health, seen));
        }

        List<ScreenSpot?> ring = new();

        foreach (JsonNode? node in root["ring"]?.AsArray() ?? new JsonArray())
        {
            ring.Add(Spot(node));
        }

        return new TargetsReport(targets) { Feet = Spot(root["feet"]), Ring = ring };
    }

    private static InteractablesReport Interactables(JsonObject root)
    {
        List<NearbyThing> things = new();

        // As with targets, one thing described badly is passed over rather than costing the rest.
        foreach (JsonNode? node in root["list"]?.AsArray() ?? new JsonArray())
        {
            if (node is not JsonObject thing || !Has(thing, "id", out uint id) || id == 0 || !Has(thing, "x", out double x) || !Has(thing, "y", out double y) || !Has(thing, "kind", out string? named))
            {
                continue;
            }

            ThingKind kind;

            switch (named)
            {
                case "loot":
                    kind = ThingKind.Loot;
                    break;
                case "talk":
                    kind = ThingKind.Talk;
                    break;
                case "use":
                    kind = ThingKind.Use;
                    break;
                default:
                    continue;
            }

            Has(thing, "distance", out double distance);
            things.Add(new NearbyThing(id, kind, x, y, distance));
        }

        return new InteractablesReport(things);
    }

    private static JsonNode Thing(NearbyThing thing)
    {
        return new JsonObject
        {
            ["id"] = thing.Id,
            ["kind"] = thing.Kind.ToString().ToLowerInvariant(),
            ["x"] = thing.X,
            ["y"] = thing.Y,
            ["distance"] = thing.Distance,
        };
    }

    private static bool Has<T>(JsonObject item, string name, out T value)
    {
        value = default!;
        return item[name] is JsonValue found && found.TryGetValue(out value!);
    }

    private static ScreenSpot? Spot(JsonNode? node)
    {
        if (node is not JsonArray pair || pair.Count != 2)
        {
            return null;
        }

        return new ScreenSpot(pair[0]!.GetValue<double>(), pair[1]!.GetValue<double>());
    }

    private static JsonNode? Spot(ScreenSpot? spot)
    {
        if (spot is null)
        {
            return null;
        }

        return new JsonArray(spot.X, spot.Y);
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

/// <summary>
/// Every enemy the game would let a skill hit within reach of the player, sent
/// whenever one moves, with where the ground round the character falls on the
/// screen: its feet, and a ring of points a few steps out, every eighth of a
/// turn from the way the camera looks. A point behind the camera is null.
/// </summary>
public sealed record TargetsReport(IReadOnlyList<SeenTarget> Targets) : GameReport
{
    public ScreenSpot? Feet { get; init; }

    public IReadOnlyList<ScreenSpot?> Ring { get; init; } = Array.Empty<ScreenSpot?>();

    // Records compare lists by reference, so equality is spelt out for the reports read back in tests.
    public bool Equals(TargetsReport? other)
    {
        return other is not null && Targets.SequenceEqual(other.Targets) && Feet == other.Feet && Ring.SequenceEqual(other.Ring);
    }

    public override int GetHashCode()
    {
        return Targets.Count;
    }
}

/// <summary>What the in-game part found it can do, said once as it connects: whether it can steer the character from the stick.</summary>
public sealed record AbilitiesReport(bool AnalogMovement) : GameReport;

/// <summary>What a click on a nearby thing does: pick it up, talk to it, or use it.</summary>
public enum ThingKind
{
    Loot,
    Talk,
    Use,
}

/// <summary>Something close by that a click would pick up, talk to or use, drawn on the screen as fractions of the window.</summary>
public sealed record NearbyThing(uint Id, ThingKind Kind, double X, double Y, double Distance);

/// <summary>Everything within reach of the character to pick up, talk to or use, in view, sent whenever it changes.</summary>
public sealed record InteractablesReport(IReadOnlyList<NearbyThing> Things) : GameReport
{
    // Records compare lists by reference, so equality is spelt out for the reports read back in tests.
    public bool Equals(InteractablesReport? other)
    {
        return other is not null && Things.SequenceEqual(other.Things);
    }

    public override int GetHashCode()
    {
        return Things.Count;
    }
}

/// <summary>A point on the game's screen, as fractions of its window from the top left.</summary>
public sealed record ScreenSpot(double X, double Y);
