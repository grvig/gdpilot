using System.Globalization;

namespace GDPilot.Core;

/// <summary>
/// Reads the two values grid calibration depends on from the game's own
/// settings file, whose lines look like "uiScale = 0.5". Only reading: the tool
/// never writes the game's settings.
/// </summary>
public static class GameSettings
{
    /// <summary>The resolution and UI scale, or null if either line is missing or unreadable.</summary>
    public static (int Width, int Height, double UiScale)? Parse(string text)
    {
        int? width = null;
        int? height = null;
        double? uiScale = null;

        foreach (string line in text.Split('\n'))
        {
            string[] halves = line.Split('=', 2, StringSplitOptions.TrimEntries);

            if (halves.Length != 2)
            {
                continue;
            }

            if (halves[0] == "resolution")
            {
                string[] sizes = halves[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (sizes.Length == 2 && int.TryParse(sizes[0], out int parsedWidth) && int.TryParse(sizes[1], out int parsedHeight))
                {
                    width = parsedWidth;
                    height = parsedHeight;
                }
            }
            else if (halves[0] == "uiScale" && double.TryParse(halves[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedScale))
            {
                uiScale = parsedScale;
            }
        }

        if (width is null || height is null || uiScale is null)
        {
            return null;
        }

        return (width.Value, height.Value, uiScale.Value);
    }
}
