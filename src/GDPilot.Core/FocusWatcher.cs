namespace GDPilot.Core;

/// <summary>
/// Suspends injection while the game is not in front and resumes it on return.
/// The caller polls; only a change in focus does anything, so a poll that finds
/// nothing new costs nothing and releases nothing.
/// </summary>
public sealed class FocusWatcher
{
    private readonly Func<bool> gameInFront;
    private readonly Suspension suspension;
    private bool? wasInFront;

    public FocusWatcher(Func<bool> gameInFront, Suspension suspension)
    {
        this.gameInFront = gameInFront;
        this.suspension = suspension;
    }

    public void Poll()
    {
        bool inFront = gameInFront();

        if (wasInFront == inFront)
        {
            return;
        }

        wasInFront = inFront;

        if (inFront)
        {
            suspension.Resume(SuspendReason.FocusLost);
        }
        else
        {
            suspension.Suspend(SuspendReason.FocusLost);
        }
    }
}
