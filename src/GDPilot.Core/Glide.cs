namespace GDPilot.Core;

/// <summary>
/// Follows a point the game reports thirty times a second at the loop's own
/// rate. Between reports the point carries on at the speed the last two showed,
/// for no longer than the gap between them, so a running enemy is followed
/// smoothly rather than in steps, and one that stops overshoots by a single
/// report's worth at most.
/// </summary>
public sealed class Glide
{
    private (double X, double Y) last;
    private (double X, double Y) speed;
    private TimeSpan sinceReport;
    private TimeSpan gap;
    private bool started;

    /// <summary>A new reading of the point.</summary>
    public void Report(double x, double y)
    {
        if (started && sinceReport > TimeSpan.Zero)
        {
            double seconds = sinceReport.TotalSeconds;
            speed = ((x - last.X) / seconds, (y - last.Y) / seconds);
            gap = sinceReport;
        }
        else
        {
            speed = (0, 0);
            gap = TimeSpan.Zero;
        }

        last = (x, y);
        sinceReport = TimeSpan.Zero;
        started = true;
    }

    /// <summary>Where the point is believed to be once this much more time has passed.</summary>
    public (double X, double Y) Advance(TimeSpan elapsed)
    {
        sinceReport += elapsed;
        double ahead = Math.Min(sinceReport.TotalSeconds, gap.TotalSeconds);
        return (last.X + speed.X * ahead, last.Y + speed.Y * ahead);
    }

    /// <summary>Forgets the point, for a new one that must not inherit its speed.</summary>
    public void Reset()
    {
        started = false;
        speed = (0, 0);
        gap = TimeSpan.Zero;
        sinceReport = TimeSpan.Zero;
    }
}
