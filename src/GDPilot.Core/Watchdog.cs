namespace GDPilot.Core;

/// <summary>
/// Polls a <see cref="StallDetector"/> from its own thread and acts when the
/// main loop stops ticking. The action is normally a release of every held key,
/// which the stalled loop can no longer do for itself.
/// </summary>
public sealed class Watchdog : IDisposable
{
    private readonly StallDetector detector;
    private readonly TimeSpan pollInterval;
    private readonly Action onStall;
    private readonly ManualResetEventSlim stopping = new();
    private readonly Thread thread;

    public Watchdog(StallDetector detector, TimeSpan pollInterval, Action onStall)
    {
        this.detector = detector;
        this.pollInterval = pollInterval;
        this.onStall = onStall;
        thread = new Thread(Run);
        thread.IsBackground = true;
        thread.Start();
    }

    /// <summary>The most recent exception thrown by the stall action, if any.</summary>
    public Exception? LastFailure { get; private set; }

    public void Dispose()
    {
        stopping.Set();
        thread.Join();
        stopping.Dispose();
    }

    private void Run()
    {
        while (!stopping.Wait(pollInterval))
        {
            if (!detector.CheckForStall())
            {
                continue;
            }

            // A failed release must not end the watchdog, or the next stall
            // would find nobody watching. It is recorded and the loop goes on.
            try
            {
                onStall();
            }
            catch (Exception failure)
            {
                LastFailure = failure;
            }
        }
    }
}
