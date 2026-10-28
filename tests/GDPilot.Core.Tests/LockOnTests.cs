using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class LockOnTests
{
    private static readonly StickPosition Still = StickPosition.Centre;
    private static readonly TimeSpan Frame = TimeSpan.FromMilliseconds(4);

    private static SeenTarget Enemy(uint id, double x, double y, double distance = 8, bool onScreen = true)
    {
        return new SeenTarget(id, x, y, distance, 100, onScreen);
    }

    [Fact]
    public void APressLocksOntoTheEnemyNearestTheMiddleOfTheView()
    {
        LockOn lockOn = new(new LockSettings());
        SeenTarget[] seen = { Enemy(1, 0.2, 0.4), Enemy(2, 0.52, 0.42), Enemy(3, 0.9, 0.5) };

        Assert.Equal(LockChange.Locked, lockOn.Toggle(seen, 0.5, 0.4));
        Assert.Equal(2u, lockOn.Target!.Id);
    }

    [Fact]
    public void AMuchNearerEnemyWinsOverOneSlightlyMoreCentral()
    {
        LockOn lockOn = new(new LockSettings());
        SeenTarget[] seen = { Enemy(1, 0.5, 0.4, distance: 24), Enemy(2, 0.56, 0.42, distance: 3) };

        lockOn.Toggle(seen, 0.5, 0.4);

        Assert.Equal(2u, lockOn.Target!.Id);
    }

    [Fact]
    public void EnemiesOutOfViewOrOutOfReachAreNeverPicked()
    {
        LockOn lockOn = new(new LockSettings(Range: 25));
        SeenTarget[] seen = { Enemy(1, 0.5, 0.4, onScreen: false), Enemy(2, 0.5, 0.4, distance: 30) };

        Assert.Equal(LockChange.None, lockOn.Toggle(seen, 0.5, 0.4));
        Assert.Null(lockOn.Target);
    }

    [Fact]
    public void ASecondPressLetsGo()
    {
        LockOn lockOn = new(new LockSettings());
        SeenTarget[] seen = { Enemy(1, 0.5, 0.4) };
        lockOn.Toggle(seen, 0.5, 0.4);

        Assert.Equal(LockChange.Released, lockOn.Toggle(seen, 0.5, 0.4));
        Assert.Null(lockOn.Target);
    }

    [Fact]
    public void TheLockFollowsItsEnemyAcrossTheScreen()
    {
        LockOn lockOn = new(new LockSettings());
        lockOn.Toggle(new[] { Enemy(1, 0.5, 0.4) }, 0.5, 0.4);

        lockOn.Update(new[] { Enemy(2, 0.5, 0.4), Enemy(1, 0.7, 0.3) }, Still, Frame);

        Assert.Equal(1u, lockOn.Target!.Id);
        Assert.Equal(0.7, lockOn.Target.X);
    }
}
