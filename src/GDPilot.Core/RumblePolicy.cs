namespace GDPilot.Core;

/// <summary>One buzz of the pad: the heavy and light motors from 0 to 1, for a length of time.</summary>
public readonly record struct Rumble(double Heavy, double Light, TimeSpan Length);

/// <summary>
/// When the pad should rumble: a jolt when a heavy blow lands, a heartbeat that
/// quickens as life runs out, and the lightest of ticks as lock-on takes or
/// changes its enemy. Health feels like a warning, so it says nothing while
/// health is full or unknown.
/// </summary>
public sealed class RumblePolicy
{
    /// <summary>Losing this many points between two reports counts as a heavy blow.</summary>
    public const int HeavyBlow = 10;

    public const int Low = 30;
    public const int Critical = 15;

    private static readonly TimeSpan LowBeat = TimeSpan.FromMilliseconds(1200);
    private static readonly TimeSpan CriticalBeat = TimeSpan.FromMilliseconds(700);

    /// <summary>
    /// The tick for a lock taken, switched or lost, on the light motor alone so it
    /// is felt in the fingers rather than the palms. Letting go on purpose needs
    /// no telling.
    /// </summary>
    public static Rumble? ForLock(LockChange change)
    {
        switch (change)
        {
            case LockChange.Locked:
                return new Rumble(0, 0.45, TimeSpan.FromMilliseconds(60));
            case LockChange.Switched:
                return new Rumble(0, 0.3, TimeSpan.FromMilliseconds(40));
            case LockChange.Lost:
                return new Rumble(0.25, 0, TimeSpan.FromMilliseconds(90));
            default:
                return null;
        }
    }

    private int last = -1;
    private TimeSpan sinceBeat;

    /// <param name="health">The player's life in percent, or -1 when unknown.</param>
    public Rumble? Tick(int health, TimeSpan elapsed)
    {
        sinceBeat += elapsed;
        int previous = last;
        last = health;

        if (health <= 0)
        {
            return null;
        }

        if (previous > 0 && previous - health >= HeavyBlow)
        {
            sinceBeat = TimeSpan.Zero;
            return new Rumble(0.8, 0.6, TimeSpan.FromMilliseconds(180));
        }

        if (health < Critical && sinceBeat >= CriticalBeat)
        {
            sinceBeat = TimeSpan.Zero;
            return new Rumble(0.6, 0, TimeSpan.FromMilliseconds(120));
        }

        if (health < Low && sinceBeat >= LowBeat)
        {
            sinceBeat = TimeSpan.Zero;
            return new Rumble(0.35, 0, TimeSpan.FromMilliseconds(110));
        }

        return null;
    }
}
