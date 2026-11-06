namespace GDPilot.Core;

/// <summary>
/// Finds the replies in a conversation box. The speaker's name and every reply
/// are drawn in gold, the speech in white, and the first two start at the same
/// place, just right of their icons. The box is translucent, so how dark it is
/// depends on the ground behind it, and the reply under the cursor is banded in
/// olive; but gold text starting at that one place is not something the world
/// draws by chance. The first such line is the name, and the rest are replies.
/// </summary>
public static class DialogueReader
{
    /// <summary>How far past the area's left edge a line of the box's text may start.</summary>
    private const int StartsWithin = 14;

    /// <summary>Text stands 12 to 24 rows tall; thinner is the box's gilded trim, and taller is the world behind it.</summary>
    private const int ShortestLine = 10;

    private const int TallestLine = 30;

    /// <summary>How far into a line the cursor goes, onto its first words.</summary>
    private const int TextInset = 60;

    /// <param name="left">Where the name and reply text start, just right of their icons.</param>
    /// <param name="width">Twice as wide as the text to read.</param>
    public static List<ScreenPoint> Choices(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int left, int top, int width, int height, double scale = 1)
    {
        List<(int First, int Last)> lines = BoxLines(pixel, left, top, width, height, scale);
        List<ScreenPoint> choices = new();

        // The first gold line is the speaker's name.
        for (int index = 1; index < lines.Count; index++)
        {
            choices.Add(new ScreenPoint(left + UiFactor.Sized(TextInset, scale), (lines[index].First + lines[index].Last) / 2));
        }

        return choices;
    }

    /// <summary>Whether a conversation's box is on screen at all, which is whether its speaker's name is.</summary>
    public static bool Showing(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int left, int top, int width, int height, double scale = 1)
    {
        return BoxLines(pixel, left, top, width, height, scale).Count > 0;
    }

    private static List<(int First, int Last)> BoxLines(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int left, int top, int width, int height, double scale)
    {
        int span = width / 2;
        int shortest = UiFactor.Sized(ShortestLine, scale);
        int tallest = UiFactor.Sized(TallestLine, scale);
        int startsWithin = UiFactor.Sized(StartsWithin, scale);
        List<(int First, int Last)> all = TextLines.Find(pixel, left, top, span, height, IsGold, scale);
        List<(int First, int Last)> lines = new();

        // Upwards from the bottom, past the trim: nothing but the box's own lines lie between its
        // foot and the name, so the first thing that is not one is the world above the box.
        for (int index = all.Count - 1; index >= 0; index--)
        {
            int tall = all[index].Last - all[index].First + 1;

            if (tall < shortest && lines.Count == 0)
            {
                continue;
            }

            if (tall < shortest || tall > tallest || Start(pixel, left, span, all[index]) > startsWithin)
            {
                break;
            }

            lines.Insert(0, all[index]);
        }

        return lines;
    }

    /// <summary>The middle row's worth of how far in a line's gold begins, so a stray pixel left of it does not count.</summary>
    private static int Start(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int left, int span, (int First, int Last) line)
    {
        List<int> starts = new();

        for (int y = line.First; y <= line.Last; y++)
        {
            for (int x = 0; x < span; x++)
            {
                if (IsGold(pixel(left + x, y)))
                {
                    starts.Add(x);
                    break;
                }
            }
        }

        starts.Sort();
        return starts[starts.Count / 2];
    }

    private static bool IsGold((byte Red, byte Green, byte Blue) colour)
    {
        (byte red, byte green, byte blue) = colour;
        return red >= 150 && green >= 110 && blue <= green * 0.7 && red - blue >= 80;
    }
}
