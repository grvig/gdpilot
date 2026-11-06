namespace GDPilot.Core;

/// <summary>Which edge of the window a piece of the interface keeps its distance from, along one axis.</summary>
public enum UiEdge
{
    Start,
    Middle,
    End,
}

/// <summary>
/// Moves a place measured at one display to where the game draws it at another.
/// Every window keeps its shape and scales about what it is anchored to: most
/// about the middle of the window, the hotbar about the foot, the journal about
/// the left, measured across displays on 2026-11-07.
/// </summary>
public readonly record struct UiGeometry(double Scale, int ReferenceWidth, int ReferenceHeight, int Left, int Top, int Width, int Height)
{
    public static UiGeometry Between(int referenceWidth, int referenceHeight, double referenceUiScale, LiveDisplay live)
    {
        double scale = UiFactor.Of(live.Width, live.Height, live.UiScale) / UiFactor.Of(referenceWidth, referenceHeight, referenceUiScale);
        return new UiGeometry(scale, referenceWidth, referenceHeight, live.Left, live.Top, live.Width, live.Height);
    }

    public double X(double x, UiEdge edge)
    {
        return Left + Along(x, ReferenceWidth, Width, edge);
    }

    public double Y(double y, UiEdge edge)
    {
        return Top + Along(y, ReferenceHeight, Height, edge);
    }

    private double Along(double place, int reference, int size, UiEdge edge)
    {
        switch (edge)
        {
            case UiEdge.Start:
                return place * Scale;
            case UiEdge.End:
                return size - (reference - place) * Scale;
            default:
                return size / 2.0 + (place - reference / 2.0) * Scale;
        }
    }
}
