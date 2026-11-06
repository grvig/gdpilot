namespace GDPilot.Core;

/// <summary>A part of the game window, as fractions of its width and height from the top left.</summary>
public readonly record struct ScreenArea(double Left, double Top, double Right, double Bottom)
{
    /// <summary>This area, measured at the reference display, where the game draws it at another. Which edges it keeps to are the part's own.</summary>
    public ScreenArea At(UiGeometry geometry, UiEdge across, UiEdge down)
    {
        return new ScreenArea(
            (geometry.X(Left * geometry.ReferenceWidth, across) - geometry.Left) / geometry.Width,
            (geometry.Y(Top * geometry.ReferenceHeight, down) - geometry.Top) / geometry.Height,
            (geometry.X(Right * geometry.ReferenceWidth, across) - geometry.Left) / geometry.Width,
            (geometry.Y(Bottom * geometry.ReferenceHeight, down) - geometry.Top) / geometry.Height);
    }

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

    // The devotion map fills the screen; what must stay clear on it is the affinities down the left and the tabs and zoom along the top, measured on 2026-11-04.
    private static readonly ScreenArea Affinities = new(0, 0.2, 0.12, 0.8);
    private static readonly ScreenArea DevotionBar = new(0.32, 0, 0.68, 0.15);
    private static readonly ScreenArea Map = new(0.24, 0.16, 0.77, 0.85);
    private static readonly ScreenArea Talk = new(0.2, 0.35, 0.8, 0.95);
    private static readonly ScreenArea GameMenu = new(0.34, 0.5, 0.66, 0.72);

    // Measured on 2026-11-05: the journal down the left, the middle windows, and a question.
    private static readonly ScreenArea JournalPanel = new(0, 0.17, 0.2, 0.84);
    private static readonly ScreenArea MiddleWindow = new(0.22, 0.18, 0.78, 0.8);
    private static readonly ScreenArea FilterWindow = new(0.22, 0.18, 0.8, 0.9);
    private static readonly ScreenArea Question = new(0.4, 0.4, 0.6, 0.62);

    // The game's tips for a new character come up top left, measured on 2026-11-05.
    private static readonly ScreenArea Tips = new(0, 0.03, 0.21, 0.17);
    private static readonly ScreenArea FrontMenu = new(0, 0.5, 0.15, 1);
    private static readonly ScreenArea FrontButtons = new(0.28, 0.87, 0.72, 0.96);

    private const int MeasuredWidth = 1920;
    private const int MeasuredHeight = 1080;
    private const double MeasuredUiScale = 0.5;

    /// <summary>Everything the game draws in a mode at the given display, as fractions of its window.</summary>
    public static IReadOnlyList<ScreenArea> Reserved(Mode mode, LiveDisplay? display)
    {
        if (display is not LiveDisplay live || live.Width <= 0 || live.Height <= 0)
        {
            return Reserved(mode);
        }

        UiGeometry geometry = UiGeometry.Between(MeasuredWidth, MeasuredHeight, MeasuredUiScale, live);
        return Reserved(mode).Select(area => At(area, geometry)).ToList();
    }

    private static ScreenArea At(ScreenArea area, UiGeometry geometry)
    {
        return area.At(geometry, EdgeOf(area.Left, area.Right), EdgeOf(area.Top, area.Bottom));
    }

    /// <summary>An area up against one side of the window stays there; the rest stand in the middle, as the game's windows do.</summary>
    private static UiEdge EdgeOf(double from, double to)
    {
        if (from <= NearTheSide)
        {
            return UiEdge.Start;
        }

        if (to >= 1 - NearTheSide)
        {
            return UiEdge.End;
        }

        return UiEdge.Middle;
    }

    private const double NearTheSide = 0.05;

    /// <summary>Everything the game draws in a mode, the world's own interface included, at the display these were measured at.</summary>
    public static IReadOnlyList<ScreenArea> Reserved(Mode mode)
    {
        if (mode == Mode.MainMenu)
        {
            return new List<ScreenArea> { FrontMenu, FrontButtons };
        }

        // The devotion map covers the world's own interface along with the world.
        if (mode == Mode.Devotion)
        {
            return new List<ScreenArea> { Affinities, DevotionBar };
        }

        List<ScreenArea> areas = new() { Minimap, Quests, Hotbar, Corner, Tips };

        switch (mode)
        {
            case Mode.Inventory:
            case Mode.Character:
                areas.Add(Bags);
                break;
            case Mode.Stash:
            case Mode.Transfer:
            case Mode.Vendor:
            case Mode.Crafting:
                areas.Add(BagsBesideAnother);
                break;
            case Mode.Skills:
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
            case Mode.Journal:
                areas.Add(JournalPanel);
                break;
            case Mode.Factions:
            case Mode.Achievements:
            case Mode.Service:
                areas.Add(MiddleWindow);
                break;
            case Mode.LootFilter:
                areas.Add(FilterWindow);
                break;
            case Mode.Confirm:
                areas.Add(Question);
                break;
        }

        return areas;
    }

    /// <summary>
    /// Where the overlay's top left goes: the preferred corner if the overlay
    /// would cover nothing of the game's there, else the first other corner
    /// that covers nothing, else the corner that covers least.
    /// </summary>
    public static (int X, int Y) Place(ScreenRect game, int width, int height, OverlayCorner preferred, Mode mode, LiveDisplay? display = null)
    {
        IReadOnlyList<ScreenArea> reserved = Reserved(mode, display);
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
