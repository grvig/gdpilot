namespace GDPilot.Core;

/// <summary>
/// What each part of the interface is anchored to, by its name in the profile,
/// and moving a whole grid or area of spots by that. Measured across displays on
/// 2026-11-07. Anything not named here floats about the middle of the window.
/// </summary>
public static class UiAnchors
{
    /// <summary>Which edges a place in a named part keeps its distance from. A place decides it for itself where a part spans the window.</summary>
    public static (UiEdge Across, UiEdge Down) Of(string name, double x, double y)
    {
        switch (name)
        {
            case "Hotbar":
            case "Assign":
                return (UiEdge.Middle, UiEdge.End);
            case "Devotion" when x < AffinitiesPane:
                return (UiEdge.Start, UiEdge.Middle);
            case "Devotion":
            case "Devotion.Tabs":
                return (UiEdge.Middle, UiEdge.Start);
            case "Journal":
                return (UiEdge.Start, UiEdge.Middle);
            case "MainMenu" when x < MainMenuLeftPane:
                return (UiEdge.Start, UiEdge.End);
            case "MainMenu":
                return (UiEdge.Middle, UiEdge.End);
            default:
                return (UiEdge.Middle, UiEdge.Middle);
        }
    }

    /// <summary>On the main menu, the list of characters and the buttons under it keep to the left corner, and the rest to the foot's middle.</summary>
    private const double MainMenuLeftPane = 300;

    /// <summary>On the devotion map, the affinities keep to the left, and the bar of tabs and zoom to the top's middle.</summary>
    private const double AffinitiesPane = 300;

    public static GridSpec Moved(GridSpec grid, string name, UiGeometry geometry)
    {
        (UiEdge across, UiEdge down) = Of(name, grid.OriginX, grid.OriginY);

        return grid with
        {
            OriginX = geometry.X(grid.OriginX, across),
            OriginY = geometry.Y(grid.OriginY, down),
            PitchX = grid.PitchX * geometry.Scale,
            PitchY = grid.PitchY * geometry.Scale,
            Scale = geometry.Scale,
        };
    }

    public static SpotArea Moved(SpotArea area, string name, UiGeometry geometry)
    {
        (UiEdge across, UiEdge down) = Of(name, area.Left, area.Top);
        double scale = geometry.Scale;

        if (area.Kind == SpotKind.Stars)
        {
            return OverTheWholeWindow(area, name, geometry);
        }

        return area with
        {
            Left = (int)Math.Round(geometry.X(area.Left, across)),
            Top = (int)Math.Round(geometry.Y(area.Top, down)),
            Width = (int)Math.Round(area.Width * scale),
            Height = (int)Math.Round(area.Height * scale),
            Look = area.Look?.At(scale),
            Fixed = area.Fixed.Select(place => Moved(place, name, geometry)).ToList(),
            Lit = area.Lit.Select(place => Moved(place, name, geometry)).ToList(),
            When = Moved(area.When, name, geometry),
            Scale = scale,
            Apart = (int)Math.Round(area.Apart * scale),
        };
    }

    /// <summary>
    /// The devotion map is drawn at one size whatever the display, centred, and shows
    /// more of itself on a larger window, measured on 2026-11-07: only what lies over
    /// it is scaled. Its stars are looked for everywhere that leaves clear.
    /// </summary>
    private static SpotArea OverTheWholeWindow(SpotArea area, string name, UiGeometry geometry)
    {
        double scale = geometry.Scale;
        int left = (int)Math.Round(area.Left * scale);
        int top = (int)Math.Round(area.Top * scale);
        int right = (int)Math.Round((geometry.ReferenceWidth - area.Left - area.Width) * scale);
        int bottom = (int)Math.Round((geometry.ReferenceHeight - area.Top - area.Height) * scale);

        return area with
        {
            Left = geometry.Left + left,
            Top = geometry.Top + top,
            Width = Math.Max(0, geometry.Width - left - right),
            Height = Math.Max(0, geometry.Height - top - bottom),
            Fixed = area.Fixed.Select(place => Moved(place, name, geometry)).ToList(),
            Lit = area.Lit.Select(place => Moved(place, name, geometry)).ToList(),
            When = Moved(area.When, name, geometry),
        };
    }

    private static ScreenPoint Moved(ScreenPoint place, string name, UiGeometry geometry)
    {
        (UiEdge across, UiEdge down) = Of(name, place.X, place.Y);
        return new ScreenPoint((int)Math.Round(geometry.X(place.X, across)), (int)Math.Round(geometry.Y(place.Y, down)));
    }

    private static FrameLine? Moved(FrameLine? line, string name, UiGeometry geometry)
    {
        if (line is not FrameLine frame)
        {
            return null;
        }

        (UiEdge across, UiEdge down) = Of(name, frame.X, frame.Top);
        return new FrameLine((int)Math.Round(geometry.X(frame.X, across)), (int)Math.Round(geometry.Y(frame.Top, down)), (int)Math.Round(geometry.Y(frame.Bottom, down)), geometry.Scale);
    }
}
