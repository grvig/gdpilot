namespace GDPilot.Core;

/// <summary>
/// Finds the rift gates on the game's world map. Each is the same small picture
/// drawn over the land, so it is told by how closely a sparse sample of that
/// picture matches, read from a real map at 1920 by 1080, UI scale 0.5, on
/// 2026-11-05. Every gate on that map differed by 17 or less, out of 255; the
/// nearest thing that was not a gate, by 35.
/// </summary>
public static class RiftGateFinder
{
    private const double MostDifference = 25;
    private const int Spacing = 20;

    /// <summary>Each sample's place from the picture's middle, and its colour.</summary>
    private static readonly (int X, int Y, byte Red, byte Green, byte Blue)[] Picture =
    {
        (-7, -9, 2, 2, 2), (-5, -9, 169, 70, 48), (-3, -9, 182, 92, 50), (-1, -9, 79, 25, 24), (1, -9, 118, 36, 35), (3, -9, 177, 79, 49), (5, -9, 202, 130, 57), (7, -9, 10, 6, 4),
        (-7, -6, 21, 10, 8), (-5, -6, 161, 54, 48), (-3, -6, 57, 27, 19), (-1, -6, 170, 51, 51), (1, -6, 179, 79, 49), (3, -6, 164, 48, 48), (5, -6, 223, 153, 63), (7, -6, 51, 16, 15),
        (-7, -3, 95, 35, 29), (-5, -3, 22, 12, 9), (-3, -3, 153, 45, 45), (-1, -3, 245, 209, 151), (1, -3, 247, 203, 144), (3, -3, 182, 80, 50), (5, -3, 181, 83, 50), (7, -3, 24, 12, 10),
        (-7, 0, 57, 24, 18), (-5, 0, 24, 13, 10), (-3, 0, 72, 26, 22), (-1, 0, 203, 107, 56), (1, 0, 246, 206, 146), (3, 0, 161, 58, 47), (5, 0, 171, 51, 51), (7, 0, 37, 17, 13),
        (-7, 3, 3, 2, 2), (-5, 3, 15, 8, 6), (-3, 3, 137, 52, 39), (-1, 3, 71, 24, 22), (1, 3, 164, 63, 46), (3, 3, 75, 32, 23), (5, 3, 115, 35, 35), (7, 3, 84, 26, 26),
        (-7, 6, 70, 101, 53), (-5, 6, 3, 3, 3), (-3, 6, 155, 59, 44), (-1, 6, 152, 59, 43), (1, 6, 123, 41, 36), (3, 6, 151, 59, 42), (5, 6, 127, 48, 37), (7, 6, 14, 19, 9),
        (-7, 9, 65, 93, 54), (-5, 9, 110, 143, 93), (-3, 9, 46, 71, 57), (-1, 9, 3, 3, 2), (1, 9, 3, 3, 2), (3, 9, 25, 34, 15), (5, 9, 84, 115, 50), (7, 9, 82, 108, 50),
    };

    public static List<ScreenPoint> Find(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int left, int top, int width, int height)
    {
        List<(double Difference, int X, int Y)> matches = new();
        double allowed = MostDifference * Picture.Length * 3;

        for (int y = top; y < top + height; y++)
        {
            for (int x = left; x < left + width; x++)
            {
                double difference = 0;

                foreach ((int dx, int dy, byte red, byte green, byte blue) in Picture)
                {
                    (byte r, byte g, byte b) = pixel(x + dx, y + dy);
                    difference += Math.Abs(r - red) + Math.Abs(g - green) + Math.Abs(b - blue);

                    // Almost everywhere gives itself away within a sample or two.
                    if (difference > allowed)
                    {
                        break;
                    }
                }

                if (difference <= allowed)
                {
                    matches.Add((difference, x, y));
                }
            }
        }

        matches.Sort((first, second) => first.Difference.CompareTo(second.Difference));
        List<ScreenPoint> gates = new();

        foreach ((double _, int x, int y) in matches)
        {
            if (!gates.Any(gate => (gate.X - x) * (gate.X - x) + (gate.Y - y) * (gate.Y - y) <= Spacing * Spacing))
            {
                gates.Add(new ScreenPoint(x, y));
            }
        }

        return gates;
    }
}
