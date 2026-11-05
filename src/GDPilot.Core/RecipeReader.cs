namespace GDPilot.Core;

/// <summary>
/// Finds the recipes a crafter lists. Each is a line of cyan text in the dark
/// list down the crafter's window, as seen with Duncan; clicking a line's text
/// picks its recipe.
/// </summary>
public static class RecipeReader
{
    /// <summary>How far into a line the cursor goes, onto its name past the count in brackets.</summary>
    private const int TextInset = 50;

    /// <summary>Text stands at least 10 rows tall; thinner is a frame's trim.</summary>
    private const int ShortestLine = 8;

    public static List<ScreenPoint> Recipes(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int left, int top, int width, int height)
    {
        return TextLines.Find(pixel, left, top, width, height, IsCyan)
            .Select(line => new ScreenPoint(left + TextInset, (line.First + line.Last) / 2))
            .ToList();
    }

    /// <summary>Every line of a list of plain text, whatever its colour, as in the journal.</summary>
    public static List<ScreenPoint> Lines(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int left, int top, int width, int height)
    {
        return TextLines.Find(pixel, left, top, width, height, IsBright)
            .Where(line => line.Last - line.First + 1 >= ShortestLine)
            .Select(line => new ScreenPoint(left + TextInset, (line.First + line.Last) / 2))
            .ToList();
    }

    private static bool IsBright((byte Red, byte Green, byte Blue) colour)
    {
        (byte red, byte green, byte blue) = colour;
        return red + green + blue >= 360;
    }

    private static bool IsCyan((byte Red, byte Green, byte Blue) colour)
    {
        (byte red, byte green, byte blue) = colour;
        return green >= 140 && blue >= 140 && red <= 120;
    }
}
