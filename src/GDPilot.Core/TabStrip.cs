namespace GDPilot.Core;

/// <summary>
/// Which tab of a strip is selected, read off the screen. The game glows the
/// selected tab red in every window that has tabs, and remembers each window's
/// tab across closing it and across clicks with the cursor, so counting presses
/// would drift. A tab's own icon can hold a little red, so the winner must beat
/// the rest clearly.
/// </summary>
public static class TabStrip
{
    /// <summary>The selected tab measured 52 to 276 red pixels; the most on any other, 10.</summary>
    private const int FewestRed = 30;

    private const int ClearlyMore = 3;

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

        if (bestRed < FewestRed || bestRed < runnerUp * ClearlyMore)
        {
            return null;
        }

        return best;
    }

    private static int RedIn(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, GridSpec strip, GridCell cell, bool column)
    {
        (int centreX, int centreY) = strip.Centre(cell);
        int halfWidth = ColumnHalfWidth;
        int halfHeight = (int)(strip.PitchY * 0.4);

        if (!column)
        {
            halfWidth = Math.Min((int)(strip.PitchX * 0.4), RowHalfWidthMost);
            halfHeight = RowHalfHeight;
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

/// <summary>A piece of a window's frame, a bright upright line, whose showing proves that window is the one on screen.</summary>
public readonly record struct FrameLine(int X, int Top, int Bottom)
{
    /// <summary>The frames measured 120 and up along most of their length; what lies under them, far less.</summary>
    private const double Bright = 90;

    /// <summary>Whether the line is lit along most of its length. Frames run a pixel or two wide, so a small span either side is searched.</summary>
    public bool Seen(Func<int, int, (byte Red, byte Green, byte Blue)> pixel)
    {
        int lit = 0;
        int rows = 0;

        for (int y = Top; y <= Bottom; y += 2)
        {
            rows++;

            for (int x = X - 2; x <= X + 2; x++)
            {
                (byte red, byte green, byte blue) = pixel(x, y);

                if ((red + green + blue) / 3.0 >= Bright)
                {
                    lit++;
                    break;
                }
            }
        }

        return lit * 10 >= rows * 8;
    }
}
