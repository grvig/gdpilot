namespace GDPilot.Core;

/// <summary>
/// Which tab of a strip is selected, read off the screen. The game glows the
/// selected tab red in every window that has tabs, and remembers each window's
/// tab across closing it and across clicks with the cursor, so counting presses
/// would drift. A tab's own icon can hold a little red, so the winner must beat
/// the rest clearly. The Transfer stash glows nothing: there the selected tab's
/// top edge goes dark, 41 to 49 against 78 to 83, measured on 2026-11-05.
/// </summary>
public static class TabStrip
{
    /// <summary>The selected tab measured 52 to 276 red pixels; the most on any other, 10.</summary>
    private const int FewestRed = 30;

    private const int ClearlyMore = 3;
    private const double ClearlyDarker = 0.75;

    private const int RowHalfHeight = 11;
    private const int RowHalfWidthMost = 50;
    private const int ColumnHalfWidth = 15;

    /// <summary>The selected tab's place along the strip, or null when none stands out.</summary>
    public static int? Selected(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, GridSpec strip)
    {
        int count = Math.Max(strip.Columns, strip.Rows);
        bool column = strip.Columns == 1;
        int best = -1;
        int bestRed = 0;
        int runnerUp = 0;

        for (int tab = 0; tab < count; tab++)
        {
            GridCell cell = new(tab, 0);

            if (column)
            {
                cell = new GridCell(0, tab);
            }

            int red = RedIn(pixel, strip, cell, column);

            if (red > bestRed)
            {
                runnerUp = bestRed;
                bestRed = red;
                best = tab;
            }
            else if (red > runnerUp)
            {
                runnerUp = red;
            }
        }

        if (bestRed < FewestRed * strip.Scale * strip.Scale || bestRed < runnerUp * ClearlyMore)
        {
            if (column)
            {
                return DarkTopped(pixel, strip, count);
            }

            return null;
        }

        return best;
    }

    private static int? DarkTopped(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, GridSpec strip, int count)
    {
        int best = -1;
        double darkest = double.MaxValue;
        double next = double.MaxValue;

        for (int tab = 0; tab < count; tab++)
        {
            (int centreX, int centreY) = strip.Centre(new GridCell(0, tab));
            double sum = 0;
            int pixels = 0;
            int across = UiFactor.Sized(20, strip.Scale);

            for (int x = centreX - across; x < centreX + across; x++)
            {
                for (int y = centreY - UiFactor.Sized(14, strip.Scale); y < centreY - UiFactor.Sized(11, strip.Scale); y++)
                {
                    (byte r, byte g, byte b) = pixel(x, y);
                    sum += (r + g + b) / 3.0;
                    pixels++;
                }
            }

            double mean = sum / pixels;

            if (mean < darkest)
            {
                next = darkest;
                darkest = mean;
                best = tab;
            }
            else if (mean < next)
            {
                next = mean;
            }
        }

        if (count < 2 || darkest > next * ClearlyDarker)
        {
            return null;
        }

        return best;
    }

    private static int RedIn(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, GridSpec strip, GridCell cell, bool column)
    {
        (int centreX, int centreY) = strip.Centre(cell);
        int halfWidth = UiFactor.Sized(ColumnHalfWidth, strip.Scale);
        int halfHeight = (int)(strip.PitchY * 0.4);

        if (!column)
        {
            halfWidth = Math.Min((int)(strip.PitchX * 0.4), UiFactor.Sized(RowHalfWidthMost, strip.Scale));
            halfHeight = UiFactor.Sized(RowHalfHeight, strip.Scale);
        }

        int red = 0;

        for (int x = centreX - halfWidth; x < centreX + halfWidth; x++)
        {
            for (int y = centreY - halfHeight; y < centreY + halfHeight; y++)
            {
                (byte r, byte g, byte b) = pixel(x, y);

                if (r >= 90 && r >= g * 2 && r >= b * 2)
                {
                    red++;
                }
            }
        }

        return red;
    }
}

/// <summary>A piece of a window's frame, a thin upright line, whose showing proves that window is the one on screen.</summary>
public readonly record struct FrameLine(int X, int Top, int Bottom)
{
    /// <summary>The frames stood 70 to 170 above what lies either side of them; the world rarely does, row after row.</summary>
    private const double StandsOut = 45;

    private const int Side = 3;

    /// <summary>Whether the line stands out along most of its length. Frames run a pixel or two wide, so a small span is searched.</summary>
    public bool Seen(Func<int, int, (byte Red, byte Green, byte Blue)> pixel)
    {
        int lit = 0;
        int rows = 0;

        for (int y = Top; y <= Bottom; y += 2)
        {
            rows++;

            for (int x = X - 2; x <= X + 2; x++)
            {
                double sides = (Brightness(pixel(x - Side, y)) + Brightness(pixel(x + Side, y))) / 2;

                if (Brightness(pixel(x, y)) - sides >= StandsOut)
                {
                    lit++;
                    break;
                }
            }
        }

        return lit * 10 >= rows * 8;
    }

    private static double Brightness((byte Red, byte Green, byte Blue) colour)
    {
        return (colour.Red + colour.Green + colour.Blue) / 3.0;
    }
}
