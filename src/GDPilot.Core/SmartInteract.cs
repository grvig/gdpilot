namespace GDPilot.Core;

/// <summary>
/// "interact:Space": with something close by to pick up, talk to or use, a
/// press clicks it; with nothing, the button holds its key as it always did.
/// </summary>
public sealed record InteractBinding(KeyCode Fallback) : ButtonBinding;

/// <summary>
/// Smart interact: one button for the obvious thing within reach, as action
/// games offer it, without aiming the cursor at it. The game does the rest: a
/// click on a thing walks the character over and picks it up, talks or uses it.
/// </summary>
public static class SmartInteract
{
    /// <summary>How near a thing must be, in world units, to be offered; the in-game part reports out to ten.</summary>
    public const double Reach = 6;

    /// <summary>
    /// The thing to offer: the nearest within reach. Loot wins a near tie, since
    /// picking up is what a fight leaves to do.
    /// </summary>
    public static NearbyThing? Offer(IReadOnlyList<NearbyThing> things)
    {
        NearbyThing? best = null;
        double bestScore = double.MaxValue;

        foreach (NearbyThing thing in things)
        {
            if (thing.Distance > Reach)
            {
                continue;
            }

            double score = thing.Distance;

            if (thing.Kind == ThingKind.Loot)
            {
                score -= 0.75;
            }

            if (score < bestScore)
            {
                best = thing;
                bestScore = score;
            }
        }

        return best;
    }

    /// <summary>What the prompt says a press would do.</summary>
    public static string Verb(ThingKind kind)
    {
        switch (kind)
        {
            case ThingKind.Loot:
                return "Pick up";
            case ThingKind.Talk:
                return "Talk";
            default:
                return "Use";
        }
    }
}
