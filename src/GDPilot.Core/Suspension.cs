namespace GDPilot.Core;

[Flags]
public enum SuspendReason
{
    None = 0,
    KillSwitch = 1,
    FocusLost = 2,
}

/// <summary>
/// Injection stops for more than one reason, and each reason clears on its own.
/// The game coming back to the front must not undo a kill switch the person
/// pressed on purpose, so injection resumes only once every reason has cleared.
/// </summary>
public sealed class Suspension
{
    private readonly GuardedSink sink;
    private readonly object gate = new();
    private SuspendReason reasons;

    public Suspension(GuardedSink sink)
    {
        this.sink = sink;
    }

    public SuspendReason Reasons
    {
        get
        {
            lock (gate)
            {
                return reasons;
            }
        }
    }

    public void Suspend(SuspendReason reason)
    {
        lock (gate)
        {
            reasons |= reason;
            sink.Disable();
        }
    }

    public void Resume(SuspendReason reason)
    {
        lock (gate)
        {
            reasons &= ~reason;

            if (reasons == SuspendReason.None)
            {
                sink.Enable();
            }
        }
    }
}
