using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class RumblePolicyTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(100);

    private static List<Rumble> Run(RumblePolicy policy, int health, TimeSpan time)
    {
        List<Rumble> felt = new();

        for (TimeSpan passed = TimeSpan.Zero; passed < time; passed += Tick)
        {
            if (policy.Tick(health, Tick) is Rumble rumble)
            {
                felt.Add(rumble);
            }
        }

        return felt;
    }

    [Fact]
    public void FullOrUnknownHealthIsQuiet()
    {
        Assert.Empty(Run(new RumblePolicy(), 100, TimeSpan.FromSeconds(5)));
        Assert.Empty(Run(new RumblePolicy(), -1, TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void AHeavyBlowJoltsOnce()
    {
        RumblePolicy policy = new();
        policy.Tick(90, Tick);

        Rumble? jolt = policy.Tick(70, Tick);

        Assert.Equal(0.8, jolt!.Value.Heavy);
        Assert.Null(policy.Tick(70, Tick));
    }

    [Fact]
    public void ASmallHitIsNotABlow()
    {
        RumblePolicy policy = new();
        policy.Tick(90, Tick);

        Assert.Null(policy.Tick(85, Tick));
    }

    [Fact]
    public void TheHeartbeatQuickensAsLifeRunsOut()
    {
        int low = Run(new RumblePolicy(), 25, TimeSpan.FromSeconds(6)).Count;
        int critical = Run(new RumblePolicy(), 10, TimeSpan.FromSeconds(6)).Count;

        Assert.InRange(low, 4, 5);
        Assert.InRange(critical, 7, 9);
    }

    [Fact]
    public void TheDeadFeelNothing()
    {
        RumblePolicy policy = new();
        policy.Tick(40, Tick);

        Assert.Null(policy.Tick(0, Tick));
    }
}
