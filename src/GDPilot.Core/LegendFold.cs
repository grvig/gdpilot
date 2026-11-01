namespace GDPilot.Core;

/// <summary>
/// When the overlay's button list folds away: it is shown in full for a few
/// seconds after the mode changes, or anything about the input is suspended
/// or resumed, then folds to the card's title and status lines.
/// </summary>
public sealed class LegendFold
{
    public static readonly TimeSpan ShownFor = TimeSpan.FromSeconds(5);

    private string? shownFor;
    private TimeSpan sinceChange;

    /// <summary>Whether the legend should be folded this tick.</summary>
    public bool Folded(OverlayState state, TimeSpan elapsed)
    {
        string now = state.Mode + "|" + state.Suspended;

        if (now != shownFor)
        {
            shownFor = now;
            sinceChange = TimeSpan.Zero;
            return false;
        }

        sinceChange += elapsed;
        return sinceChange >= ShownFor;
    }
}
