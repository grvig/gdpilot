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
