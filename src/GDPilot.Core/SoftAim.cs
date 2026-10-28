namespace GDPilot.Core;

/// <summary>
/// Soft aim, for fighting without a lock: in third person the cursor drifts
/// from the aim point onto an enemy close to where the camera points, so an
/// attack swung roughly at an enemy lands on it, the way action games turn a
/// swing toward the foe in front. Only enemies near the aim point and near the
/// character count, and the one already chosen is kept until it leaves a
/// wider circle, so the cursor never flickers between two.
/// </summary>
public sealed class SoftAim
{
    /// <summary>How near an enemy must be, in world units.</summary>
    private readonly double range;

    /// <summary>How far from the aim point it may be drawn, in screen heights.</summary>
    private readonly double reach;

    public SoftAim(double range = 12, double reach = 0.14)
    {
        this.range = range;
        this.reach = reach;
    }

    /// <summary>The enemy the cursor drifts onto, or null to wait at the aim point.</summary>
    public SeenTarget? Current { get; private set; }

    public SeenTarget? Pick(IReadOnlyList<SeenTarget> seen, double aimX, double aimY, double aspect)
    {
        SeenTarget? best = null;
        double bestScore = double.MaxValue;

        foreach (SeenTarget target in seen)
        {
            double apart = Math.Sqrt(Math.Pow((target.X - aimX) * aspect, 2) + Math.Pow(target.Y - aimY, 2));
            double allowed = reach;

            if (Current is not null && target.Id == Current.Id)
            {
                allowed = reach * 1.5;
            }

            if (!target.OnScreen || target.Distance > range || apart > allowed)
            {
                continue;
            }

            double score = apart / reach + 0.5 * target.Distance / range;

            // The one already chosen wins a near tie.
            if (Current is not null && target.Id == Current.Id)
            {
                score -= 0.3;
            }

            if (score < bestScore)
            {
                best = target;
                bestScore = score;
            }
        }

        Current = best;
        return best;
    }

    public void Forget()
    {
        Current = null;
    }
}
