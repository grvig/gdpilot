namespace GDPilot.Core;

/// <summary>Cursor speeds in pixels per second. Names follow section 7's configuration.</summary>
public sealed record CursorSettings(double BaseSpeed, double Exponent, double MaxSpeed);

/// <summary>
/// Turns the right stick into relative cursor moves. The fraction of a pixel
/// left over each tick is carried into the next, because at 250 Hz a gentle
/// push asks for less than one pixel per tick and would otherwise never move.
/// </summary>
public sealed class CursorMotion
{
    private readonly CursorSettings settings;
    private double carryX;
    private double carryY;

    public CursorMotion(CursorSettings settings)
    {
        this.settings = settings;
    }

    public void Reset()
    {
        carryX = 0;
        carryY = 0;
    }

    /// <summary>speed = baseSpeed × magnitude^exponent, clamped to maxSpeed.</summary>
    public double SpeedFor(double magnitude)
    {
        return Math.Min(settings.BaseSpeed * Math.Pow(magnitude, settings.Exponent), settings.MaxSpeed);
    }

    /// <summary>The whole-pixel move for one tick, or null when there is none.</summary>
    public PointerMove? Step(StickPosition stick, TimeSpan elapsed)
    {
        double magnitude = stick.Magnitude;

        if (magnitude == 0)
        {
            carryX = 0;
            carryY = 0;
            return null;
        }

        double distance = SpeedFor(magnitude) * elapsed.TotalSeconds;

        // Screen Y grows downward, stick Y grows upward.
        double x = stick.X / magnitude * distance + carryX;
        double y = -stick.Y / magnitude * distance + carryY;
        int wholeX = (int)Math.Truncate(x);
        int wholeY = (int)Math.Truncate(y);
        carryX = x - wholeX;
        carryY = y - wholeY;

        if (wholeX == 0 && wholeY == 0)
        {
            return null;
        }

        return new PointerMove(wholeX, wholeY);
    }
}
