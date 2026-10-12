namespace GDPilot.Core;

/// <summary>
/// Notices when the main loop stops ticking. The loop calls <see cref="Beat"/>
/// every tick and a separate watchdog thread calls <see cref="CheckForStall"/>.
/// A stalled loop cannot release the keys it holds, so the watchdog has to.
/// </summary>
public sealed class StallDetector
{
    private readonly TimeSpan threshold;
    private readonly Func<TimeSpan> clock;
    private readonly object gate = new();
    private TimeSpan lastBeat;
    private bool reported;

    public StallDetector(TimeSpan threshold, Func<TimeSpan> clock)
    {
        this.threshold = threshold;
        this.clock = clock;
        lastBeat = clock();
    }

    public void Beat()
    {
        lock (gate)
        {
            lastBeat = clock();
            reported = false;
        }
    }

    /// <summary>
    /// True once per stall, on the first check past the threshold. Reporting on
    /// every check would release the same keys on every watchdog pass.
    /// </summary>
    public bool CheckForStall()
    {
        lock (gate)
        {
            if (reported || clock() - lastBeat <= threshold)
            {
                return false;
            }

            reported = true;
            return true;
        }
    }
}
