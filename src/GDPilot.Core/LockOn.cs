namespace GDPilot.Core;

/// <summary>What lock-on just did, for the rumble and the overlay.</summary>
public enum LockChange
{
    None,
    Locked,
    Switched,
    Released,
    Lost,
}

/// <summary>
/// Lock-on's distances, in world units: how near an enemy must be to be locked
/// onto, and how far a locked one may get before the lock breaks. The gap
/// between them keeps a lock from flickering at the edge of reach.
/// </summary>
public sealed record LockSettings(double Range = 25, double BreakRange = 32);

/// <summary>
/// Lock-on the way action games do it. A press locks onto the enemy the player
/// is most plainly facing: near the middle of the view, and near. The lock then
/// follows that enemy wherever the game reports it, until a second press lets
/// go. Everything here works on the game's reports alone, so it is tested
/// without a game.
/// </summary>
public sealed class LockOn
{
    /// <summary>How much an enemy's distance counts against it, next to how far off the middle of the view it is.</summary>
    private const double DistanceWeight = 0.35;

    private readonly LockSettings settings;

    public LockOn(LockSettings settings)
    {
        this.settings = settings;
    }

    /// <summary>The game window's width over its height, so distances across the screen are measured fairly.</summary>
    public double Aspect { get; set; } = 16.0 / 9;

    /// <summary>The enemy held, as last reported, or null.</summary>
    public SeenTarget? Target { get; private set; }

    /// <summary>
    /// Locks onto the best enemy in view, or lets go of the one held. The aim
    /// point is where the player looks, as fractions of the game window.
    /// </summary>
    public LockChange Toggle(IReadOnlyList<SeenTarget> seen, double aimX, double aimY)
    {
        if (Target is not null)
        {
            Target = null;
            return LockChange.Released;
        }

        Target = Best(seen, aimX, aimY);

        if (Target is null)
        {
            return LockChange.None;
        }

        return LockChange.Locked;
    }

    /// <summary>Lets go without a press, as when a panel opens over the world.</summary>
    public void Release()
    {
        Target = null;
    }

    /// <summary>Follows the held enemy into the latest report.</summary>
    public LockChange Update(IReadOnlyList<SeenTarget> seen, StickPosition right, TimeSpan elapsed)
    {
        if (Target is null)
        {
            return LockChange.None;
        }

        foreach (SeenTarget target in seen)
        {
            if (target.Id == Target.Id)
            {
                Target = target;
            }
        }

        return LockChange.None;
    }

    private SeenTarget? Best(IReadOnlyList<SeenTarget> seen, double aimX, double aimY)
    {
        SeenTarget? best = null;
        double bestScore = double.MaxValue;

        foreach (SeenTarget target in seen)
        {
            if (!target.OnScreen || target.Distance > settings.Range)
            {
                continue;
            }

            double score = Apart(target.X, target.Y, aimX, aimY) + DistanceWeight * target.Distance / settings.Range;

            if (score < bestScore)
            {
                best = target;
                bestScore = score;
            }
        }

        return best;
    }

    /// <summary>How far apart two points on the screen are, in screen heights.</summary>
    private double Apart(double x1, double y1, double x2, double y2)
    {
        return Math.Sqrt(Math.Pow((x1 - x2) * Aspect, 2) + Math.Pow(y1 - y2, 2));
    }
}
