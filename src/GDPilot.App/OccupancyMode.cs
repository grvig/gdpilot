using GDPilot.Core;

namespace GDPilot.App;

/// <summary>
/// Prints what the screen reading makes of one calibrated grid right now, as
/// --occupancy GridName with the panel open: # for an item, . for empty, ? for
/// unsure. It shows at a glance why presses jump or fall back to stepping.
/// </summary>
internal static class OccupancyMode
{
    internal static int Run(GDPilotConfig config, string gridName)
    {
        if (!config.Profiles.TryGetValue(config.ActiveProfile, out ProfileConfig? profile) || !profile.Grids.TryGetValue(gridName, out GridConfig? entry))
        {
            Console.Error.WriteLine($"No grid named {gridName} in the active profile.");
            return 2;
        }

        GridSpec grid = entry.ToSpec();
        OccupancyMap? map = new ScreenOccupancy().Read(grid, null);

        if (map is null)
        {
            Console.Error.WriteLine("The screen could not be captured.");
            return 1;
        }

        for (int row = 0; row < map.Rows; row++)
        {
            string line = "";

            for (int column = 0; column < map.Columns; column++)
            {
                line += Symbol(map[new GridCell(column, row)]);
            }

            Console.WriteLine(line);
        }

        if (map.IsConfident)
        {
            Console.WriteLine("Confident: presses jump between items.");
        }
        else
        {
            Console.WriteLine("Too many unsure cells: presses step one cell at a time.");
        }

        return 0;
    }

    private static string Symbol(CellState state)
    {
        switch (state)
        {
            case CellState.Occupied:
                return "#";
            case CellState.Empty:
                return ".";
            default:
                return "?";
        }
    }
}
