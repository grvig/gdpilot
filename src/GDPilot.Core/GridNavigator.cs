namespace GDPilot.Core;

/// <summary>A grid action from a binding: "grid:up", "grid:down", "grid:left", "grid:right" or "grid:next".</summary>
public sealed record GridBinding(string Action) : ButtonBinding;

/// <summary>
/// Moves the cursor cell by cell over the grids calibrated for the current mode.
/// A mode can have several surfaces, such as the main bag and the second bag,
/// named "Inventory" and "Inventory.Bag2"; "grid:next" moves between them.
/// </summary>
public sealed class GridNavigator
{
    private readonly GridReadiness readiness;
    private Mode mode = Mode.Suspended;
    private List<string> surfaces = new();
    private int surface;
    private GridCell cell;
    private bool placed;

    public GridNavigator(GridReadiness readiness)
    {
        this.readiness = readiness;
    }

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
            surfaces = readiness.Grids.Keys.Where(name => name == current.ToString() || name.StartsWith(current + ".", StringComparison.Ordinal)).Order().ToList();
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
            cell = new GridCell(0, 0);
        }
        else if (!placed)
        {
            // The first press lands on the top-left cell rather than stepping from wherever the cursor was.
            cell = new GridCell(0, 0);
        }
        else
        {
            cell = grid.Step(cell, Direction(action));
        }

        placed = true;
        Notice = null;
        (int x, int y) = grid.Centre(cell);
        return new PointerMoveTo(x, y);
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
