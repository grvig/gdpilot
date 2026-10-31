namespace GDPilot.Core;

/// <summary>
/// Rumble from the fight itself, read from the targets reports alone: a light
/// tick as an enemy close by, or the locked one, loses life, a firmer knock
/// when a single blow takes a big piece of it, and a thump as an enemy close by
/// and nearly dead disappears from the reports, which is its death. The reports
/// do not say who struck, so a companion's blow is felt too; it is the fight's
/// rhythm in the hands, not a score.
/// </summary>
public sealed class CombatRumble
{
    /// <summary>How near an enemy must be, in world units, for its wounds to be felt; a locked one is felt at any distance.</summary>
    private const double Near = 8;

    /// <summary>How near and how low an enemy must have been for its disappearing to be felt as a kill.</summary>
    private const double KillReach = 12;
    private const int KillLife = 30;

    /// <summary>A loss this large in one report is a heavy blow.</summary>
    private const int HeavyBlow = 15;

    /// <summary>Light ticks come no closer together than this, so a flurry stays a texture rather than a drone.</summary>
    private static readonly TimeSpan TickGap = TimeSpan.FromMilliseconds(120);

    private Dictionary<uint, SeenTarget> last = new();
    private IReadOnlyList<SeenTarget>? lastReport;
    private TimeSpan sinceTick = TimeSpan.MaxValue / 2;

    /// <param name="seen">The latest targets report; only a new one is read.</param>
    /// <param name="focus">The locked enemy's id, or null.</param>
    public Rumble? Update(IReadOnlyList<SeenTarget> seen, uint? focus, TimeSpan elapsed)
    {
        sinceTick += elapsed;

        if (ReferenceEquals(seen, lastReport))
        {
            return null;
        }

        lastReport = seen;
        Dictionary<uint, SeenTarget> now = new();
        int biggest = 0;

        foreach (SeenTarget target in seen)
        {
            now[target.Id] = target;

            if (last.TryGetValue(target.Id, out SeenTarget? before) && (target.Distance <= Near || target.Id == focus))
            {
                biggest = Math.Max(biggest, before.Health - target.Health);
            }
        }

        bool killed = false;

        foreach (SeenTarget before in last.Values)
        {
            if (!now.ContainsKey(before.Id) && before.Health <= KillLife && before.Distance <= KillReach)
            {
                killed = true;
            }
        }

        last = now;

        if (killed)
        {
            sinceTick = TimeSpan.Zero;
            return new Rumble(0.55, 0.2, TimeSpan.FromMilliseconds(100));
        }

        if (biggest >= HeavyBlow)
        {
            sinceTick = TimeSpan.Zero;
            return new Rumble(0.4, 0.3, TimeSpan.FromMilliseconds(60));
        }

        if (biggest > 0 && sinceTick >= TickGap)
        {
            sinceTick = TimeSpan.Zero;
            return new Rumble(0, 0.18, TimeSpan.FromMilliseconds(30));
        }

        return null;
    }
}
