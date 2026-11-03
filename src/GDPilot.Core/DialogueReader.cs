namespace GDPilot.Core;

/// <summary>
/// Finds the replies in a conversation box. The box is a near-black panel
/// along the bottom of the screen that grows upwards with its text; the
/// speaker's name and every reply are drawn in gold, the speech in white. So
/// the replies are the gold lines inside the dark run of rows at the bottom,
/// after the first, which is the name. Clicking a reply's text picks it.
/// </summary>
public static class DialogueReader
{
    /// <summary>The empty right of the box measured 6 to 17; the town around it, 45 and up.</summary>
    private const double BoxDarkness = 22;

    /// <summary>Gold pixels in a row for it to be part of a line of text.</summary>
    private const int GoldPerRow = 6;

    /// <summary>Rows without gold that a line may span, for the gaps inside letters.</summary>
    private const int LineGap = 3;

    /// <summary>Box rows with text in them may be brighter; this many in a row still count as the box.</summary>
    private const int BoxGap = 12;

    /// <summary>How far into a line the cursor goes, onto its first words.</summary>
    private const int TextInset = 60;

    /// <param name="left">Where reply text starts, just right of the reply icons.</param>
    /// <param name="width">Wide enough to reach the box's empty right, where its darkness is judged.</param>
    public static List<ScreenPoint> Choices(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int left, int top, int width, int height)
    {
        List<ScreenPoint> choices = new();
        (int boxTop, int boxBottom) = Box(pixel, left, top, width, height);

        if (boxTop < 0)
        {
            return choices;
        }

        // The box's gilded edge lies just outside its dark rows, so only those rows are read.
        List<(int First, int Last)> lines = GoldLines(pixel, left, boxTop, width / 2, boxBottom - boxTop + 1);

        // The first gold line is the speaker's name.
        for (int index = 1; index < lines.Count; index++)
        {
            choices.Add(new ScreenPoint(left + TextInset, (lines[index].First + lines[index].Last) / 2));
        }

        return choices;
    }

    /// <summary>The first and last rows of the dark box rising from the bottom of the area, or -1 when there is none.</summary>
    private static (int Top, int Bottom) Box(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int left, int top, int width, int height)
    {
        int right = left + width;
        int sampleFrom = right - width / 3;
        int boxTop = -1;
        int boxBottom = -1;
        int bright = 0;

        for (int y = top + height - 1; y >= top; y--)
        {
            if (MedianBrightness(pixel, sampleFrom, right, y) < BoxDarkness)
            {
                boxTop = y;
                bright = 0;

                if (boxBottom < 0)
                {
                    boxBottom = y;
                }

                continue;
            }

            bright++;

            // Either the bottom of the area is not a box at all, or the box has ended.
            if (bright > BoxGap)
            {
                break;
            }
        }

        return (boxTop, boxBottom);
    }

    private static List<(int First, int Last)> GoldLines(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int left, int top, int width, int height)
    {
        List<(int First, int Last)> lines = new();
        int first = -1;
        int last = -1;

        for (int y = top; y < top + height; y++)
        {
            int gold = 0;

            for (int x = left; x < left + width; x++)
            {
                if (IsGold(pixel(x, y)))
                {
                    gold++;
                }
            }

            if (gold >= GoldPerRow)
            {
                if (first < 0)
                {
                    first = y;
                }

                last = y;
            }
            else if (first >= 0 && y - last > LineGap)
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

    private static bool IsGold((byte Red, byte Green, byte Blue) colour)
    {
        (byte red, byte green, byte blue) = colour;
        return red >= 150 && green >= 110 && blue <= green * 0.7 && red - blue >= 80;
    }

    private static double MedianBrightness(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int from, int to, int y)
    {
        List<double> values = new();

        for (int x = from; x < to; x += 2)
        {
            (byte red, byte green, byte blue) = pixel(x, y);
            values.Add((red + green + blue) / 3.0);
        }

        values.Sort();
        return values[values.Count / 2];
    }
}
