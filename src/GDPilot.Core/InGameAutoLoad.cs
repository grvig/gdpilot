namespace GDPilot.Core;

/// <summary>
/// When to load the in-game part without being asked: once for each run of the
/// game, after it has had time to finish starting. A load that fails is not tried
/// again until the game restarts, since the same game would fail the same way.
/// </summary>
public sealed class InGameAutoLoad
{
    /// <summary>Time for the game to finish loading its own libraries, which the in-game part looks through.</summary>
    public static readonly TimeSpan Settle = TimeSpan.FromSeconds(20);

    private int? process;
    private TimeSpan seen;
    private bool settled;

    /// <param name="processId">The running game, or null when there is none.</param>
    /// <param name="connected">Whether the in-game part is already talking.</param>
    public bool Due(int? processId, bool connected, TimeSpan now)
    {
        if (processId != process)
        {
            process = processId;
            seen = now;
            settled = false;
        }

        if (process is null || settled)
        {
            return false;
        }

        if (connected)
        {
            settled = true;
            return false;
        }

        if (now - seen < Settle)
        {
            return false;
        }

        settled = true;
        return true;
    }

    /// <summary>Whether there is nothing left to do for this run of the game, so the look for it can be spared.</summary>
    public bool Settled => settled;
}
