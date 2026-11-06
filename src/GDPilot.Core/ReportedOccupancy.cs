namespace GDPilot.Core;

/// <summary>
/// Occupancy from the game's own reports when the in-game part is running, and
/// from the fallback, normally the screen, when it is not or has not described
/// a grid yet. Reports arrive on the connection's thread and are read on the
/// control loop's, so every access is locked.
/// </summary>
public sealed class ReportedOccupancy : IOccupancySource
{
    private readonly IOccupancySource? fallback;
    private readonly Dictionary<string, LayoutReport> layouts = new();
    private readonly object guard = new();

    public ReportedOccupancy(IOccupancySource? fallback)
    {
        this.fallback = fallback;
    }

    public void Accept(LayoutReport report)
    {
        lock (guard)
        {
            layouts[report.Grid] = report;
        }
    }

    /// <summary>The tab the game last said a grid is showing, or null when it has not.</summary>
    public int? TabOf(string grid)
    {
        lock (guard)
        {
            if (layouts.TryGetValue(grid, out LayoutReport? report))
            {
                return report.Tab;
            }

            return null;
        }
    }

    /// <summary>How many tabs the game says a grid's window has, whether it reports them whole or one by one.</summary>
    public int? TabsOf(string grid)
    {
        lock (guard)
        {
            if (layouts.TryGetValue(grid, out LayoutReport? report) || layouts.TryGetValue(grid + "#0", out report))
            {
                return report.Tabs;
            }

            return null;
        }
    }

    /// <summary>Drops every report, for when the in-game part disconnects and what it said can no longer be trusted.</summary>
    public void Forget()
    {
        lock (guard)
        {
            layouts.Clear();
        }
    }

    public OccupancyMap? Read(string name, GridSpec grid, OccupancyMap? previous)
    {
        LayoutReport? report;

        lock (guard)
        {
            layouts.TryGetValue(name, out report);
        }

        // A report sized differently from the calibrated grid would put items in the wrong cells.
        // A trader's tab not yet shown has its items unplaced, all of no size, until the game lays them out.
        if (report is not null && report.Columns == grid.Columns && report.Rows == grid.Rows && report.Items.All(item => item.Width > 0 && item.Height > 0))
        {
            return OccupancyMap.FromItems(report.Columns, report.Rows, report.Items);
        }

        if (fallback is null)
        {
            return null;
        }

        return fallback.Read(name, grid, previous);
    }
}
