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
        if (report is not null && report.Columns == grid.Columns && report.Rows == grid.Rows)
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
