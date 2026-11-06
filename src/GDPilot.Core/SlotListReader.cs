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

    /// <summary>Drawn at any other size the edge falls across two columns and loses up to half its height, measured on 2026-11-07: 59 to 95 above nothing, 17 to 62 above its sides, and less steady.</summary>
    private const double ResizedEdgeDimmest = 50;
    private const double ResizedEdgeSteadiness = 20;
    private const double ResizedEdgeAboveSides = 15;

    /// <summary>Where the text runs, from the list's left, past each row's icon.</summary>
    private const int TextFrom = 40;
    private const int TextTo = 230;
    private const int TextPixelsAtLeast = 12;
    private const double TextBright = 150;

    /// <summary>Text is crisp: most of what is lit at all is bright. Sunlit ground over which a list ends is the other way about, measured at 17 to 32 in a hundred against 80 and up.</summary>
    private const double TextLit = 110;
    private const double TextCrispness = 0.6;
    private const int TextHalfHeight = 7;
    private const int StopInset = 60;

    /// <param name="height">The area's last row is where the list's foot is looked for.</param>
    public static List<ScreenPoint> Rows(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int left, int top, int width, int height, double scale = 1)
    {
        List<ScreenPoint> rows = new();
        int foot = top + height - 1;
        int listWidth = UiFactor.Sized(ListWidth, scale);
        int halfHeight = UiFactor.Sized(TextHalfHeight, scale);

        for (int edge = left + listWidth; edge < left + width; edge++)
        {
            if (EdgeBrightness(pixel, edge, foot, scale) is not double edgeLook)
            {
                continue;
            }

            int listLeft = edge - listWidth;
            double steadiness = EdgeSteadiness;

            if (scale != 1)
            {
                steadiness = ResizedEdgeSteadiness;
            }

            for (int row = 0; ; row++)
            {
                int y = foot - (int)Math.Round((FirstRowAbove + row * RowHeight) * scale);

                if (y - halfHeight < top)
                {
                    break;
                }

                // Over bright ground, text alone cannot say where the list stops; its edge can.
                if (Math.Abs(Brightness(pixel(edge, y)) - edgeLook) > steadiness * 2 || !HasText(pixel, listLeft, y, scale))
                {
                    break;
                }

                rows.Insert(0, new ScreenPoint(listLeft + UiFactor.Sized(StopInset, scale), y));
            }

            return rows;
        }

        return rows;
    }

    /// <summary>The line's brightness if this column is the list's edge, or null if it is not.</summary>
    private static double? EdgeBrightness(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int x, int foot, double scale)
    {
        double total = 0;
        double sides = 0;
        int checkedRows = UiFactor.Sized(EdgeRowsChecked, scale);
        int first = foot - UiFactor.Sized(EdgeRowsAboveFoot, scale) - checkedRows;
        double[] values = new double[checkedRows];

        for (int index = 0; index < checkedRows; index++)
        {
            values[index] = Brightness(pixel(x, first + index));
            total += values[index];
            sides += (Brightness(pixel(x - 2, first + index)) + Brightness(pixel(x + 2, first + index))) / 2;
        }

        double mean = total / checkedRows;
        double dimmest = EdgeDimmest;
        double aboveSides = EdgeAboveSides;
        double steadiness = EdgeSteadiness;

        if (scale != 1)
        {
            dimmest = ResizedEdgeDimmest;
            aboveSides = ResizedEdgeAboveSides;
            steadiness = ResizedEdgeSteadiness;
        }

        if (mean < dimmest || mean > EdgeBrightest || mean - sides / checkedRows < aboveSides)
        {
            return null;
        }

        if (values.All(value => Math.Abs(value - mean) <= steadiness))
        {
            return mean;
        }

        return null;
    }

    private static bool HasText(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int listLeft, int y, double scale)
    {
        int bright = 0;
        int lit = 0;
        int halfHeight = UiFactor.Sized(TextHalfHeight, scale);

        for (int x = listLeft + UiFactor.Sized(TextFrom, scale); x < listLeft + UiFactor.Sized(TextTo, scale); x++)
        {
            for (int row = y - halfHeight; row <= y + halfHeight; row++)
            {
                double brightness = Brightness(pixel(x, row));

                if (brightness >= TextBright)
                {
                    bright++;
                }

                if (brightness >= TextLit)
                {
                    lit++;
                }
            }
        }

        return bright >= TextPixelsAtLeast * scale * scale && bright >= lit * TextCrispness;
    }

    private static double Brightness((byte Red, byte Green, byte Blue) colour)
    {
        return (colour.Red + colour.Green + colour.Blue) / 3.0;
    }
}
