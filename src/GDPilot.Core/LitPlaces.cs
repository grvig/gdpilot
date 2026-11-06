namespace GDPilot.Core;

/// <summary>
/// Places on screen that are stops only while the game lights them red, as the
/// attributes' plus buttons are with points to spend and Undo Class is just after
/// choosing. Measured on 2026-11-05: 59 red pixels on a lit plus, 0 with none to
/// spend; a lit Undo Class fills the area, and the art behind it holds none.
/// </summary>
public static class LitPlaces
{
    public const int Reach = 7;

    private const int FewestRed = 30;

    public static bool Lit(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, ScreenPoint place)
    {
        int red = 0;

        for (int x = place.X - Reach; x <= place.X + Reach; x++)
        {
            for (int y = place.Y - Reach; y <= place.Y + Reach; y++)
            {
                (byte r, byte g, byte b) = pixel(x, y);

                if (r >= 45 && r >= g * 2.2 && r >= b * 2.2)
                {
                    red++;
                }
            }
        }

        return red >= FewestRed;
    }

    /// <summary>A screen with the given rectangle blacked out, as GDPilot's own card lies over the game but is no part of it.</summary>
    public static Func<int, int, (byte Red, byte Green, byte Blue)> Without(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, ScreenRect? hidden)
    {
        if (hidden is not ScreenRect rect)
        {
            return pixel;
        }

        return (x, y) =>
        {
            if (x >= rect.X && x < rect.X + rect.Width && y >= rect.Y && y < rect.Y + rect.Height)
            {
                return (0, 0, 0);
            }

            return pixel(x, y);
        };
    }
}
