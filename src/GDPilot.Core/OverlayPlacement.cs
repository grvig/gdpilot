namespace GDPilot.Core;

public enum OverlayCorner
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}

/// <summary>A rectangle in physical screen pixels.</summary>
public readonly record struct ScreenRect(int X, int Y, int Width, int Height);

public static class OverlayPlacement
{
    /// <summary>Gap between the overlay and the game window's edges, in pixels.</summary>
    public const int Margin = 16;

    /// <summary>Where the overlay's top-left goes so it sits in the chosen corner of the game.</summary>
    public static (int X, int Y) Place(ScreenRect game, int overlayWidth, int overlayHeight, OverlayCorner corner)
    {
        int left = game.X + Margin;
        int right = game.X + game.Width - overlayWidth - Margin;
        int top = game.Y + Margin;
        int bottom = game.Y + game.Height - overlayHeight - Margin;

        switch (corner)
        {
            case OverlayCorner.TopRight:
                return (right, top);
            case OverlayCorner.BottomLeft:
                return (left, bottom);
            case OverlayCorner.BottomRight:
                return (right, bottom);
            default:
                return (left, top);
        }
    }
}

/// <summary>The "overlay" section of the configuration.</summary>
public sealed class OverlayConfig
{
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Top left by default: Grim Dawn keeps its minimap top right, its quest
    /// tracker down the right side and its health and skill bars along the bottom.
    /// </summary>
    public OverlayCorner Corner { get; init; } = OverlayCorner.TopLeft;
}
