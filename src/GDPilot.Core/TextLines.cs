namespace GDPilot.Core;

/// <summary>
/// Finds lines of text of one colour in an area, row by row: a row with
/// enough pixels of the colour is part of a line, and a line ends after a
/// few rows without, which bridges the gaps inside letters.
/// </summary>
public static class TextLines
{
    /// <summary>Pixels of the colour in a row for it to be part of a line of text.</summary>
    private const int PerRow = 6;

    /// <summary>Rows without the colour that a line may span, for the gaps inside letters.</summary>
    private const int Gap = 3;

    /// <summary>Each line's first and last rows, from the top.</summary>
    public static List<(int First, int Last)> Find(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int left, int top, int width, int height, Func<(byte Red, byte Green, byte Blue), bool> isText, double scale = 1)
    {
        List<(int First, int Last)> lines = new();
        int perRow = UiFactor.Sized(PerRow, scale);
        int gap = UiFactor.Sized(Gap, scale);
        int first = -1;
        int last = -1;

        for (int y = top; y < top + height; y++)
        {
            int count = 0;

            for (int x = left; x < left + width; x++)
            {
                if (isText(pixel(x, y)))
                {
                    count++;
                }
            }

            if (count >= perRow)
            {
                if (first < 0)
                {
                    first = y;
                }

                last = y;
            }
            else if (first >= 0 && y - last > gap)
            {
                lines.Add((first, last));
                first = -1;
            }
        }

        if (first >= 0)
        {
            lines.Add((first, last));
        }

        return lines;
    }
}
