namespace GDPilot.Core;

/// <summary>A part of the game window, as fractions of its width and height from the top left.</summary>
public readonly record struct ScreenArea(double Left, double Top, double Right, double Bottom)
{
    public bool Overlaps(ScreenArea other)
    {
        return Left < other.Right && other.Left < Right && Top < other.Bottom && other.Top < Bottom;
    }

    /// <summary>How much of this area the other covers, as a fraction of the window.</summary>
    public double Shared(ScreenArea other)
    {
        double width = Math.Min(Right, other.Right) - Math.Max(Left, other.Left);
        double height = Math.Min(Bottom, other.Bottom) - Math.Max(Top, other.Top);

        if (width <= 0 || height <= 0)
        {
            return 0;
        }

        return width * height;
    }
}

/// <summary>
/// Where Grim Dawn draws its own interface, so the overlay never covers it: the
/// minimap top right, the quest tracker down the right, the orbs and hotbar
/// along the bottom, and whichever panel the mode has open. Measured on
/// 2026-11-01 at 1920 by 1080 with the interface at half scale, the setting
/// GDPilot's grids are calibrated for, and kept as fractions so they follow the
/// window's size.
/// </summary>
public static class GameLayout
{
    private static readonly ScreenArea Minimap = new(0.845, 0, 1, 0.285);
    private static readonly ScreenArea Quests = new(0.865, 0.29, 1, 0.6);
    private static readonly ScreenArea Hotbar = new(0.265, 0.86, 0.735, 1);
    private static readonly ScreenArea Corner = new(0.97, 0.92, 1, 1);

    private static readonly ScreenArea Bags = new(0.32, 0.16, 0.68, 0.84);
    private static readonly ScreenArea BagsBesideAnother = new(0.22, 0.12, 0.8, 0.9);
    private static readonly ScreenArea SkillWindow = new(0.24, 0.19, 0.77, 0.84);
    private static readonly ScreenArea Map = new(0.24, 0.16, 0.77, 0.85);
    private static readonly ScreenArea Talk = new(0.2, 0.35, 0.8, 0.95);
    private static readonly ScreenArea GameMenu = new(0.34, 0.5, 0.66, 0.72);

    /// <summary>Everything the game draws in a mode, the world's own interface included.</summary>
    public static IReadOnlyList<ScreenArea> Reserved(Mode mode)
    {
        List<ScreenArea> areas = new() { Minimap, Quests, Hotbar, Corner };

        switch (mode)
        {
            case Mode.Inventory:
            case Mode.Character:
                areas.Add(Bags);
                break;
            case Mode.Stash:
            case Mode.Vendor:
                areas.Add(BagsBesideAnother);
                break;
            case Mode.Skills:
            case Mode.Devotion:
                areas.Add(SkillWindow);
                break;
            case Mode.WorldMap:
                areas.Add(Map);
                break;
            case Mode.Dialog:
                areas.Add(Talk);
                break;
            case Mode.Menu:
                areas.Add(GameMenu);
                break;
        }

        return areas;
    }

    /// <summary>
    /// Where the overlay's top left goes: the preferred corner if the overlay
    /// would cover nothing of the game's there, else the first other corner
    /// that covers nothing, else the corner that covers least.
    /// </summary>
    public static (int X, int Y) Place(ScreenRect game, int width, int height, OverlayCorner preferred, Mode mode)
    {
        IReadOnlyList<ScreenArea> reserved = Reserved(mode);
        List<OverlayCorner> corners = new() { preferred };

        foreach (OverlayCorner corner in Enum.GetValues<OverlayCorner>())
        {
            if (corner != preferred)
            {
                corners.Add(corner);
            }
        }

        (int X, int Y) best = OverlayPlacement.Place(game, width, height, preferred);
        double bestCovered = double.MaxValue;

        foreach (OverlayCorner corner in corners)
        {
            (int x, int y) = OverlayPlacement.Place(game, width, height, corner);
            ScreenArea card = new((double)(x - game.X) / game.Width, (double)(y - game.Y) / game.Height, (double)(x - game.X + width) / game.Width, (double)(y - game.Y + height) / game.Height);
            double covered = 0;

            foreach (ScreenArea area in reserved)
            {
                covered += card.Shared(area);
            }

            if (covered == 0)
            {
                return (x, y);
            }

            if (covered < bestCovered)
            {
                best = (x, y);
                bestCovered = covered;
            }
        }

        return best;
    }
}
