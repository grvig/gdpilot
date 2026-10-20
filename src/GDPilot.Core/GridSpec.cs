namespace GDPilot.Core;

public enum GridDirection
{
    Up,
    Down,
    Left,
    Right,
}

public readonly record struct GridCell(int Column, int Row);

/// <summary>
/// One calibrated grid surface, such as the bags or the stash. The origin is the
/// centre of the top-left cell and the pitch is the distance between the
/// centres of neighbouring cells, both in physical screen pixels.
/// </summary>
public sealed record GridSpec(double OriginX, double OriginY, double PitchX, double PitchY, int Columns, int Rows)
{
    /// <summary>A grid whose numbers are still the configuration's zero placeholders.</summary>
    public bool IsCalibrated => Columns > 0 && Rows > 0 && PitchX > 0 && PitchY > 0;

    /// <summary>
    /// Section 11's calibration: the centres of the top-left and bottom-right
    /// cells plus the counts. Pitch is the span divided by count minus one.
    /// </summary>
    public static GridSpec FromCorners(double firstX, double firstY, double lastX, double lastY, int columns, int rows)
    {
        if (columns < 2 || rows < 2)
        {
            throw new ArgumentException("A grid needs at least two columns and two rows to measure its pitch.");
        }

        double pitchX = (lastX - firstX) / (columns - 1);
        double pitchY = (lastY - firstY) / (rows - 1);

        if (pitchX <= 0 || pitchY <= 0)
        {
            throw new ArgumentException("The bottom-right cell must lie below and to the right of the top-left cell.");
        }

        return new GridSpec(firstX, firstY, pitchX, pitchY, columns, rows);
    }

    /// <summary>Where to put the cursor for a cell, rounded to a whole pixel.</summary>
    public (int X, int Y) Centre(GridCell cell)
    {
        return ((int)Math.Round(OriginX + cell.Column * PitchX), (int)Math.Round(OriginY + cell.Row * PitchY));
    }

    /// <summary>One cell over, staying put at an edge rather than wrapping or leaving the grid.</summary>
    public GridCell Step(GridCell from, GridDirection direction)
    {
        int column = from.Column;
        int row = from.Row;

        switch (direction)
        {
            case GridDirection.Up:
                row--;
                break;
            case GridDirection.Down:
                row++;
                break;
            case GridDirection.Left:
                column--;
                break;
            case GridDirection.Right:
                column++;
                break;
        }

        return new GridCell(Math.Clamp(column, 0, Columns - 1), Math.Clamp(row, 0, Rows - 1));
    }
}
