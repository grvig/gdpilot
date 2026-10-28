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
/// go. A flick of the right stick moves the lock to the next enemy that way on
/// the screen; the stick must come back toward the middle before another flick
/// counts, so one flick is always one switch. Everything here works on the
/// game's reports alone, so it is tested without a game.
///
/// When the held enemy dies, the lock moves straight on to the enemy nearest
/// where it fell, so a fight flows from one to the next. It breaks when the
/// enemy gets beyond reach or stays out of view.
/// </summary>
public sealed class LockOn
{
    /// <summary>How much an enemy's distance counts against it, next to how far off the middle of the view it is.</summary>
    private const double DistanceWeight = 0.35;

    /// <summary>How far the right stick must be flicked to switch targets, and how far back it must come before the next flick counts.</summary>
    private const double FlickThreshold = 0.7;
    private const double FlickRearm = 0.35;

    /// <summary>How far either side of a flick's direction an enemy may lie and still be the one it means.</summary>
    private static readonly double SwitchCone = Math.PI / 3;

    /// <summary>
    /// How long a held enemy may be missing from the reports before it counts as
    /// gone, so one late report never drops a lock, and how long it may stay out
    /// of view before the lock lets go.
    /// </summary>
    private static readonly TimeSpan LostGrace = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan OutOfViewLimit = TimeSpan.FromSeconds(3);

    private readonly LockSettings settings;
    private bool armed = true;
    private TimeSpan missingFor;
    private TimeSpan outOfViewFor;

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
        missingFor = TimeSpan.Zero;
        outOfViewFor = TimeSpan.Zero;

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

    /// <summary>Follows the held enemy into the latest report, and switches on a flick of the right stick.</summary>
    public LockChange Update(IReadOnlyList<SeenTarget> seen, StickPosition right, TimeSpan elapsed)
    {
        if (Target is null)
        {
            // A stick held over as the lock begins is not a flick.
            armed = right.Magnitude < FlickRearm;
            return LockChange.None;
        }

        LockChange change = Follow(seen, elapsed);

        if (Target is null || change != LockChange.None)
        {
            return change;
        }

        return Flick(seen, right);
    }

    private LockChange Follow(IReadOnlyList<SeenTarget> seen, TimeSpan elapsed)
    {
        SeenTarget held = Target!;
        SeenTarget? now = seen.FirstOrDefault(target => target.Id == held.Id);

        if (now is null)
        {
            missingFor += elapsed;

            if (missingFor < LostGrace)
            {
                return LockChange.None;
            }

            // Gone from the game's list: dead, or no longer something a skill could hit.
            Target = Nearest(seen, held);
            missingFor = TimeSpan.Zero;
            outOfViewFor = TimeSpan.Zero;

            if (Target is null)
            {
                return LockChange.Lost;
            }

            return LockChange.Switched;
        }

        missingFor = TimeSpan.Zero;
        Target = now;

        if (now.OnScreen)
        {
            outOfViewFor = TimeSpan.Zero;
        }
        else
        {
            outOfViewFor += elapsed;
        }

        if (now.Distance > settings.BreakRange || outOfViewFor >= OutOfViewLimit)
        {
            Target = null;
            return LockChange.Lost;
        }

        return LockChange.None;
    }

    /// <summary>The enemy in view and in reach nearest where another one was last seen on the screen.</summary>
    private SeenTarget? Nearest(IReadOnlyList<SeenTarget> seen, SeenTarget gone)
    {
        SeenTarget? best = null;
        double bestApart = double.MaxValue;

        foreach (SeenTarget target in seen)
        {
            if (!target.OnScreen || target.Distance > settings.Range)
            {
                continue;
            }

            double apart = Apart(target.X, target.Y, gone.X, gone.Y);

            if (apart < bestApart)
            {
                best = target;
                bestApart = apart;
            }
        }

        return best;
    }

    private LockChange Flick(IReadOnlyList<SeenTarget> seen, StickPosition right)
    {
        if (right.Magnitude < FlickRearm)
        {
            armed = true;
            return LockChange.None;
        }

        if (!armed || right.Magnitude < FlickThreshold)
        {
            return LockChange.None;
        }

        armed = false;

        // The stick's up is the screen's up, which is toward smaller fractions.
        SeenTarget? next = Neighbour(seen, Target!, right.X, -right.Y);

        if (next is null)
        {
            return LockChange.None;
        }

        Target = next;
        return LockChange.Switched;
    }

    /// <summary>
    /// The enemy a flick means: the nearest on the screen within the cone around
    /// its direction, with those far off the line counting as further away.
    /// </summary>
    private SeenTarget? Neighbour(IReadOnlyList<SeenTarget> seen, SeenTarget from, double directionX, double directionY)
    {
        SeenTarget? best = null;
        double bestScore = double.MaxValue;
        double directionLength = Math.Sqrt(directionX * directionX + directionY * directionY);

        foreach (SeenTarget target in seen)
        {
            if (target.Id == from.Id || !target.OnScreen || target.Distance > settings.Range)
            {
                continue;
            }

            double x = (target.X - from.X) * Aspect;
            double y = target.Y - from.Y;
            double length = Math.Sqrt(x * x + y * y);

            if (length < 0.0001)
            {
                continue;
            }

            double angle = Math.Acos(Math.Clamp((x * directionX + y * directionY) / (length * directionLength), -1, 1));

            if (angle > SwitchCone)
            {
                continue;
            }

            double score = length * (1 + 2 * angle / SwitchCone);

            if (score < bestScore)
            {
                best = target;
                bestScore = score;
            }
        }

        return best;
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
