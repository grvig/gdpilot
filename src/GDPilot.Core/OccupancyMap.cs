namespace GDPilot.Core;

/// <summary>Reads a grid's occupancy, by its profile name such as "Inventory.Bag2", or null when it cannot.</summary>
public interface IOccupancySource
{
    OccupancyMap? Read(string name, GridSpec grid, OccupancyMap? previous);
}

/// <summary>One item's place in a grid, in cells, as the game itself records it. Ids are never 0.</summary>
public readonly record struct ItemRect(int Id, int Column, int Row, int Width, int Height);

public enum CellState
{
    Empty,
    Occupied,
    Unsure,
}

/// <summary>A cell's middle, summarised: how bright it is and how far from grey, both from 0 to 255.</summary>
public readonly record struct CellLook(double Brightness, double Colour)
{
    // Measured on the three captures: empty cells sit between 4 and 11.3 with no
    // colour at all; the faintest colourless item, a grey gauntlet on its
    // translucent box, reads 15.3. Between the two bounds a cell is not guessed at.
    private const double EmptyBelow = 12.5;
    private const double OccupiedFrom = 14;
    private const double ColourFrom = 3;

    public CellState State
    {
        get
        {
            if (Colour >= ColourFrom || Brightness >= OccupiedFrom)
            {
                return CellState.Occupied;
            }

            if (Brightness < EmptyBelow)
            {
                return CellState.Empty;
            }

            return CellState.Unsure;
        }
    }
}

/// <summary>
/// Which cells of one grid hold something, read from the screen. Empty cells in
/// Grim Dawn are near-black and colourless inside their bright lines; anything
/// stored draws a tinted box and an icon over them.
/// </summary>
public sealed class OccupancyMap
{
    /// <summary>Past this share of unsure cells the whole reading is doubted and stepping takes over.</summary>
    private const double MostUnsure = 0.1;

    /// <summary>
    /// How much brighter than its surroundings a cell's lines must be to count as
    /// seen. Visible lines measured 46 to 96; under a tooltip, 3 to 5.
    /// </summary>
    private const double LineVisibleFrom = 20;

    private readonly CellState[,] states;

    /// <summary>Which item fills each cell, 0 for none, when that is known; null when cells were read one by one.</summary>
    private readonly int[,]? owners;

    public OccupancyMap(CellState[,] states, int[,]? owners = null)
    {
        this.states = states;
        this.owners = owners;
    }

    /// <summary>
    /// A map from the game's own record of where items are. Every cell is known,
    /// and an item larger than one cell is a single stop.
    /// </summary>
    public static OccupancyMap FromItems(int columns, int rows, IEnumerable<ItemRect> items)
    {
        CellState[,] states = new CellState[columns, rows];
        int[,] owners = new int[columns, rows];

        foreach (ItemRect item in items)
        {
            for (int column = Math.Max(0, item.Column); column < Math.Min(columns, item.Column + item.Width); column++)
            {
                for (int row = Math.Max(0, item.Row); row < Math.Min(rows, item.Row + item.Height); row++)
                {
                    states[column, row] = CellState.Occupied;
                    owners[column, row] = item.Id;
                }
            }
        }

        return new OccupancyMap(states, owners);
    }

    public int Columns => states.GetLength(0);

    public int Rows => states.GetLength(1);

    public CellState this[GridCell cell] => states[cell.Column, cell.Row];

    public bool IsConfident
    {
        get
        {
            int unsure = 0;

            foreach (CellState state in states)
            {
                if (state == CellState.Unsure)
                {
                    unsure++;
                }
            }

            return unsure <= states.Length * MostUnsure;
        }
    }

    /// <summary>The first cell holding something, reading row by row, or null in an empty grid.</summary>
    public GridCell? FirstItem()
    {
        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                if (states[column, row] == CellState.Occupied)
                {
                    return Anchor(new GridCell(column, row));
                }
            }
        }

        return null;
    }

    /// <summary>
    /// The nearest other item in a direction, by its top-left cell, or null when
    /// nothing lies that way. Drifting sideways costs three times as much as going
    /// straight, so a press stays in its row or column while that row or column
    /// has anything. Cells of the item already under the cursor are passed over.
    /// </summary>
    public GridCell? NextItem(GridCell from, GridDirection direction)
    {
        GridCell? best = null;
        int bestCost = int.MaxValue;
        int current = Owner(from);
        (GridCell topLeft, GridCell bottomRight) = Extent(from);

        for (int column = 0; column < Columns; column++)
        {
            for (int row = 0; row < Rows; row++)
            {
                if (states[column, row] != CellState.Occupied || Owner(new GridCell(column, row)) == current)
                {
                    continue;
                }

                (int along, int across) = Offset(topLeft, bottomRight, new GridCell(column, row), direction);
                int cost = along + across * 3;

                if (along > 0 && cost < bestCost)
                {
                    best = new GridCell(column, row);
                    bestCost = cost;
                }
            }
        }

        if (best is null)
        {
            return null;
        }

        return Anchor(best.Value);
    }

    /// <summary>The item filling a cell. Without the game's record, every cell counts as its own item.</summary>
    private int Owner(GridCell cell)
    {
        if (owners is not null)
        {
            return owners[cell.Column, cell.Row];
        }

        return -(cell.Row * Columns + cell.Column + 1);
    }

    /// <summary>The top-left cell of the item filling this cell, where the cursor lands on it.</summary>
    private GridCell Anchor(GridCell cell)
    {
        return Extent(cell).TopLeft;
    }

    /// <summary>The corners of the item filling this cell, or of the cell alone when items are not known.</summary>
    private (GridCell TopLeft, GridCell BottomRight) Extent(GridCell cell)
    {
        if (owners is null || owners[cell.Column, cell.Row] == 0)
        {
            return (cell, cell);
        }

        int owner = owners[cell.Column, cell.Row];
        int left = cell.Column;
        int top = cell.Row;
        int right = cell.Column;
        int bottom = cell.Row;

        for (int column = 0; column < Columns; column++)
        {
            for (int row = 0; row < Rows; row++)
            {
                if (owners[column, row] == owner)
                {
                    left = Math.Min(left, column);
                    top = Math.Min(top, row);
                    right = Math.Max(right, column);
                    bottom = Math.Max(bottom, row);
                }
            }
        }

        return (new GridCell(left, top), new GridCell(right, bottom));
    }

    /// <summary>
    /// How far a cell lies beyond the current item's edge in the direction, and
    /// how far to one side of the item's span. A cell beside a tall item is not
    /// "below" it just because it is lower than the item's top.
    /// </summary>
    private static (int Along, int Across) Offset(GridCell topLeft, GridCell bottomRight, GridCell to, GridDirection direction)
    {
        int sideways = Gap(to.Column, topLeft.Column, bottomRight.Column);
        int upright = Gap(to.Row, topLeft.Row, bottomRight.Row);

        switch (direction)
        {
            case GridDirection.Up:
                return (topLeft.Row - to.Row, sideways);
            case GridDirection.Down:
                return (to.Row - bottomRight.Row, sideways);
            case GridDirection.Left:
                return (topLeft.Column - to.Column, upright);
            default:
                return (to.Column - bottomRight.Column, upright);
        }
    }

    /// <summary>How far a position lies outside a span, 0 when inside it.</summary>
    private static int Gap(int position, int first, int last)
    {
        if (position < first)
        {
            return first - position;
        }

        if (position > last)
        {
            return position - last;
        }

        return 0;
    }

    /// <summary>
    /// Reads every cell of a grid through a pixel source in screen coordinates.
    /// Only the middle of each cell is sampled, well inside its lines, so the
    /// lines themselves never count as content.
    /// </summary>
    /// <param name="previous">
    /// The last reading of the same grid, if any. The game draws tooltips and the
    /// carried item over cells, hiding their lines; such a cell may still turn
    /// occupied, but keeps its previous state otherwise. Reading an item as
    /// empty would skip it, which is worse than one needless stop.
    /// </param>
    public static OccupancyMap Read(GridSpec grid, Func<int, int, (byte Red, byte Green, byte Blue)> pixel, OccupancyMap? previous = null)
    {
        CellState[,] states = new CellState[grid.Columns, grid.Rows];
        int reach = (int)(Math.Min(grid.PitchX, grid.PitchY) * 0.3);

        for (int column = 0; column < grid.Columns; column++)
        {
            for (int row = 0; row < grid.Rows; row++)
            {
                GridCell cell = new(column, row);
                (int x, int y) = grid.Centre(cell);
                CellState state = Look(pixel, x, y, reach).State;

                if (previous is not null && state != CellState.Occupied && !LinesVisible(grid, pixel, x, y))
                {
                    state = previous[cell];
                }

                states[column, row] = state;
            }
        }

        return new OccupancyMap(states);
    }

    /// <summary>Whether the left and top lines of the cell centred here can be seen along most of their length.</summary>
    private static bool LinesVisible(GridSpec grid, Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int centreX, int centreY)
    {
        int left = (int)Math.Round(centreX - grid.PitchX / 2);
        int top = (int)Math.Round(centreY - grid.PitchY / 2);
        int reach = (int)(Math.Min(grid.PitchX, grid.PitchY) * 0.3);
        List<double> leftContrast = new();
        List<double> topContrast = new();

        for (int along = -reach; along <= reach; along++)
        {
            leftContrast.Add(Brightness(pixel, left, centreY + along) - (Brightness(pixel, left - 3, centreY + along) + Brightness(pixel, left + 3, centreY + along)) / 2);
            topContrast.Add(Brightness(pixel, centreX + along, top) - (Brightness(pixel, centreX + along, top - 3) + Brightness(pixel, centreX + along, top + 3)) / 2);
        }

        return Median(leftContrast) >= LineVisibleFrom && Median(topContrast) >= LineVisibleFrom;
    }

    private static double Brightness(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int x, int y)
    {
        (byte red, byte green, byte blue) = pixel(x, y);
        return (red + green + blue) / 3.0;
    }

    private static double Median(List<double> values)
    {
        values.Sort();
        return values[values.Count / 2];
    }

    public static CellLook Look(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int centreX, int centreY, int reach)
    {
        double brightness = 0;
        double colour = 0;
        int count = 0;

        for (int x = centreX - reach; x <= centreX + reach; x++)
        {
            for (int y = centreY - reach; y <= centreY + reach; y++)
            {
                (byte red, byte green, byte blue) = pixel(x, y);
                brightness += (red + green + blue) / 3.0;
                colour += Math.Max(red, Math.Max(green, blue)) - Math.Min(red, Math.Min(green, blue));
                count++;
            }
        }

        return new CellLook(brightness / count, colour / count);
    }
}
