namespace GDPilot.Core;

/// <summary>
/// Finds the rows of the list the game opens on a hotbar slot. It stands on the
/// hotbar above the slot, a fixed width wide, with rows of a fixed height rising
/// from its foot for as long as they hold text. Its right edge is a thin line of
/// one steady brightness, which nothing in the world behind it is, measured on
/// 2026-11-05 at 1920 by 1080, UI scale 0.5, over slots at both ends of the bar.
/// </summary>
public static class SlotListReader
{
    private const int ListWidth = 240;
    private const int RowHeight = 34;

    /// <summary>From the list's foot up to the middle of its lowest row.</summary>
    private const int FirstRowAbove = 22;

    /// <summary>The edge is checked along this many rows, leaving the last few that blend into the hotbar's trim.</summary>
    private const int EdgeRowsChecked = 30;
    private const int EdgeRowsAboveFoot = 6;
    private const double EdgeBrightest = 170;
    private const double EdgeDimmest = 110;
    private const double EdgeSteadiness = 12;
    private const double EdgeAboveSides = 30;

    /// <summary>Where the text runs, from the list's left, past each row's icon.</summary>
    private const int TextFrom = 40;
    private const int TextTo = 230;
    private const int TextPixelsAtLeast = 12;
    private const double TextBright = 150;
    private const int TextHalfHeight = 7;
    private const int StopInset = 60;

    /// <param name="height">The area's last row is where the list's foot is looked for.</param>
    public static List<ScreenPoint> Rows(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int left, int top, int width, int height)
    {
        List<ScreenPoint> rows = new();
        int foot = top + height - 1;

        for (int edge = left + ListWidth; edge < left + width; edge++)
        {
            if (!IsEdge(pixel, edge, foot))
            {
                continue;
            }

            int listLeft = edge - ListWidth;

            double edgeLook = Brightness(pixel(edge, foot - EdgeRowsAboveFoot));

            for (int y = foot - FirstRowAbove; y - TextHalfHeight >= top; y -= RowHeight)
            {
                // Over bright ground, text alone cannot say where the list stops; its edge can.
                if (Math.Abs(Brightness(pixel(edge, y)) - edgeLook) > EdgeSteadiness * 2 || !HasText(pixel, listLeft, y))
                {
                    break;
                }

                rows.Insert(0, new ScreenPoint(listLeft + StopInset, y));
            }

            return rows;
        }

        return rows;
    }

    private static bool IsEdge(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int x, int foot)
    {
        double total = 0;
        double sides = 0;
        int first = foot - EdgeRowsAboveFoot - EdgeRowsChecked;
        double[] values = new double[EdgeRowsChecked];

        for (int index = 0; index < EdgeRowsChecked; index++)
        {
            values[index] = Brightness(pixel(x, first + index));
            total += values[index];
            sides += (Brightness(pixel(x - 2, first + index)) + Brightness(pixel(x + 2, first + index))) / 2;
        }

        double mean = total / EdgeRowsChecked;

        if (mean < EdgeDimmest || mean > EdgeBrightest || mean - sides / EdgeRowsChecked < EdgeAboveSides)
        {
            return false;
        }

        return values.All(value => Math.Abs(value - mean) <= EdgeSteadiness);
    }

    private static bool HasText(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int listLeft, int y)
    {
        int bright = 0;

        for (int x = listLeft + TextFrom; x < listLeft + TextTo; x++)
        {
            for (int row = y - TextHalfHeight; row <= y + TextHalfHeight; row++)
            {
                if (Brightness(pixel(x, row)) >= TextBright)
                {
                    bright++;
                }
            }
        }

        return bright >= TextPixelsAtLeast;
    }

    private static double Brightness((byte Red, byte Green, byte Blue) colour)
    {
        return (colour.Red + colour.Green + colour.Blue) / 3.0;
    }
}
