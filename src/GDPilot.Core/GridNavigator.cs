namespace GDPilot.Core;

/// <summary>A grid action from a binding: "grid:up", "grid:down", "grid:left", "grid:right" or "grid:next".</summary>
public sealed record GridBinding(string Action) : ButtonBinding;

/// <summary>"tab:next" or "tab:previous": clicks the neighbouring stash tab. Step is +1 or -1.</summary>
public sealed record TabBinding(int Step) : ButtonBinding;

/// <summary>
/// Moves the cursor over the grids calibrated for the current mode. A mode can
/// have several surfaces, such as the main bag and the second bag, named
/// "Inventory" and "Inventory.Bag2"; "grid:next" moves between them. With an
/// occupancy source, presses jump from item to item over empty cells; without
/// one, or when a reading is in doubt, they step one cell at a time.
/// </summary>
public sealed class GridNavigator
{
    private const string TabsSuffix = ".Tabs";

    private readonly GridReadiness readiness;
    private readonly IOccupancySource? occupancy;
    private readonly Dictionary<string, OccupancyMap> known = new();
    private int tab;
    private Mode mode = Mode.Suspended;
    private List<string> surfaces = new();
    private int surface;
    private GridCell cell;
    private bool placed;

    public GridNavigator(GridReadiness readiness, IOccupancySource? occupancy = null)
    {
        this.readiness = readiness;
        this.occupancy = occupancy;
    }

    /// <summary>Whether the last press jumped between items, rather than stepping a cell.</summary>
    public bool Jumping { get; private set; }

    /// <summary>Whether jumping is on but the last press in this mode had to step, because the screen could not be trusted.</summary>
    public bool FellBack => occupancy is not null && placed && !Jumping;

    /// <summary>Why the last grid press did nothing, or null when it worked.</summary>
    public string? Notice { get; private set; }

    /// <summary>Where the cursor should go for this press, or null when navigation is unavailable.</summary>
    public PointerMoveTo? Act(Mode current, string action)
    {
        if (!readiness.Ready)
        {
            Notice = readiness.Message;
            return null;
        }

        if (current != mode)
        {
            // Each mode starts fresh on its first surface, since its panel may sit elsewhere on screen.
            mode = current;

            // A tab strip is a one-row grid too, but it is clicked through, not walked.
            surfaces = readiness.Grids.Keys
                .Where(name => name == current.ToString() || name.StartsWith(current + ".", StringComparison.Ordinal))
                .Where(name => !name.EndsWith(TabsSuffix, StringComparison.Ordinal))
                .Order()
                .ToList();
            surface = 0;
            placed = false;
        }

        if (surfaces.Count == 0)
        {
            Notice = $"No grid is calibrated for {current}.";
            return null;
        }

        GridSpec grid = readiness.Grids[surfaces[surface]];

        if (action == "next")
        {
            surface = (surface + 1) % surfaces.Count;
            grid = readiness.Grids[surfaces[surface]];
        }

        OccupancyMap? map = Occupancy(surfaces[surface], grid);
        Jumping = map is not null;

        if (action == "next" || !placed)
        {
            // The first press lands on the first item, or the top-left cell, rather than stepping from wherever the cursor was.
            cell = map?.FirstItem() ?? new GridCell(0, 0);
        }
        else if (map is null)
        {
            cell = grid.Step(cell, Direction(action));
        }
        else
        {
            cell = map.NextItem(cell, Direction(action)) ?? cell;
        }

        placed = true;
        Notice = null;
        (int x, int y) = grid.Centre(cell);
        return new PointerMoveTo(x, y);
    }

    /// <summary>
    /// Clicks the next or previous tab, wrapping round, for a mode whose profile
    /// has a tab strip calibrated as a one-row grid named "Stash.Tabs". The game
    /// switches stash tabs only by clicking them, so a tab change is a move and a click.
    /// </summary>
    public List<InputIntent> Tab(Mode current, int step)
    {
        List<InputIntent> intents = new();

        if (!readiness.Ready)
        {
            Notice = readiness.Message;
            return intents;
        }

        if (!readiness.Grids.TryGetValue(current + TabsSuffix, out GridSpec? tabs))
        {
            Notice = $"No tabs are calibrated for {current}.";
            return intents;
        }

        // The stash draws its tabs as a column down its left edge, but a row is just as possible.
        int count = Math.Max(tabs.Columns, tabs.Rows);
        tab = ((tab + step) % count + count) % count;
        GridCell cell = new(tab, 0);

        if (tabs.Columns == 1)
        {
            cell = new GridCell(0, tab);
        }

        (int x, int y) = tabs.Centre(cell);
        intents.Add(new PointerMoveTo(x, y));
        intents.Add(new PointerDown(PointerButton.Left));
        intents.Add(new PointerUp(PointerButton.Left));
        Notice = null;
        return intents;
    }

    /// <summary>A fresh, trusted reading of a surface, or null to step instead.</summary>
    private OccupancyMap? Occupancy(string name, GridSpec grid)
    {
        if (occupancy is null)
        {
            return null;
        }

        known.TryGetValue(name, out OccupancyMap? previous);
        OccupancyMap? map = occupancy.Read(grid, previous);

        if (map is null || !map.IsConfident)
        {
            return null;
        }

        known[name] = map;
        return map;
    }

    private static GridDirection Direction(string action)
    {
        switch (action)
        {
            case "up":
                return GridDirection.Up;
            case "down":
                return GridDirection.Down;
            case "left":
                return GridDirection.Left;
            default:
                return GridDirection.Right;
        }
    }
}
