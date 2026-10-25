namespace GDPilot.Core;

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

    private readonly CellState[,] states;

    public OccupancyMap(CellState[,] states)
    {
        this.states = states;
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

    /// <summary>
    /// Reads every cell of a grid through a pixel source in screen coordinates.
    /// Only the middle of each cell is sampled, well inside its lines, so the
    /// lines themselves never count as content.
    /// </summary>
    public static OccupancyMap Read(GridSpec grid, Func<int, int, (byte Red, byte Green, byte Blue)> pixel)
    {
        CellState[,] states = new CellState[grid.Columns, grid.Rows];
        int reach = (int)(Math.Min(grid.PitchX, grid.PitchY) * 0.3);

        for (int column = 0; column < grid.Columns; column++)
        {
            for (int row = 0; row < grid.Rows; row++)
            {
                (int x, int y) = grid.Centre(new GridCell(column, row));
                states[column, row] = Look(pixel, x, y, reach).State;
            }
        }

        return new OccupancyMap(states);
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
