namespace GDPilot.Core;

/// <summary>
/// Says when what the game runs at has changed, so its places can be worked out
/// again: it can start after GDPilot, and its window and interface size can
/// change while it runs. A game that has gone leaves the last display standing,
/// since nothing is being drawn to be wrong about.
/// </summary>
public sealed class DisplayWatch
{
    private LiveDisplay? last;

    public bool Changed(LiveDisplay? now)
    {
        if (now is null || now == last)
        {
            return false;
        }

        last = now;
        return true;
    }
}
