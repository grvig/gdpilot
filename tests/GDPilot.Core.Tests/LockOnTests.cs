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
    public void ALockBindingReadsPlainlyInTheLegend()
    {
        ButtonLayers combat = BindingTable.Parse(new Dictionary<string, string> { ["RightStick"] = "lock:toggle", ["LT+RightStick"] = "camera:toggle" }, 0.5);

        Assert.IsType<LockBinding>(combat.BindingFor(GamepadButtons.RightStick, 0));
        Assert.IsType<CameraToggleBinding>(combat.BindingFor(GamepadButtons.RightStick, 1));
        Assert.Contains("RightStick: Lock on", OverlayState.Build(Mode.Combat, SuspendReason.None, combat).Legend);
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

    private static LockOn LockedOnto(SeenTarget[] seen, uint id)
    {
        LockOn lockOn = new(new LockSettings());
        SeenTarget held = seen.Single(target => target.Id == id);
        lockOn.Toggle(new[] { held }, held.X, held.Y);
        return lockOn;
    }

    [Fact]
    public void AFlickRightSwitchesToTheNextEnemyToTheRight()
    {
        SeenTarget[] seen = { Enemy(1, 0.3, 0.4), Enemy(2, 0.5, 0.4), Enemy(3, 0.62, 0.45), Enemy(4, 0.9, 0.4) };
        LockOn lockOn = LockedOnto(seen, 2);

        Assert.Equal(LockChange.Switched, lockOn.Update(seen, new StickPosition(0.9, 0), Frame));
        Assert.Equal(3u, lockOn.Target!.Id);
    }

    [Fact]
    public void AFlickLeftSwitchesLeft()
    {
        SeenTarget[] seen = { Enemy(1, 0.3, 0.4), Enemy(2, 0.5, 0.4), Enemy(3, 0.62, 0.45) };
        LockOn lockOn = LockedOnto(seen, 2);

        lockOn.Update(seen, new StickPosition(-0.9, 0), Frame);

        Assert.Equal(1u, lockOn.Target!.Id);
    }

    [Fact]
    public void AFlickUpReachesTheEnemyFurtherIntoTheView()
    {
        SeenTarget[] seen = { Enemy(1, 0.5, 0.6), Enemy(2, 0.52, 0.3), Enemy(3, 0.8, 0.6) };
        LockOn lockOn = LockedOnto(seen, 1);

        lockOn.Update(seen, new StickPosition(0, 0.9), Frame);

        Assert.Equal(2u, lockOn.Target!.Id);
    }

    [Fact]
    public void HoldingTheStickOverSwitchesOnceUntilItComesBack()
    {
        SeenTarget[] seen = { Enemy(1, 0.3, 0.4), Enemy(2, 0.5, 0.4), Enemy(3, 0.7, 0.4) };
        LockOn lockOn = LockedOnto(seen, 1);
        StickPosition right = new(0.9, 0);

        lockOn.Update(seen, right, Frame);
        lockOn.Update(seen, right, Frame);
        Assert.Equal(2u, lockOn.Target!.Id);

        lockOn.Update(seen, Still, Frame);
        lockOn.Update(seen, right, Frame);
        Assert.Equal(3u, lockOn.Target!.Id);
    }

    [Fact]
    public void AFlickWithNothingThatWayKeepsTheLock()
    {
        SeenTarget[] seen = { Enemy(1, 0.3, 0.4), Enemy(2, 0.5, 0.4) };
        LockOn lockOn = LockedOnto(seen, 2);

        Assert.Equal(LockChange.None, lockOn.Update(seen, new StickPosition(0.9, 0), Frame));
        Assert.Equal(2u, lockOn.Target!.Id);
    }

    [Fact]
    public void AGentleNudgeIsNotAFlick()
    {
        SeenTarget[] seen = { Enemy(1, 0.5, 0.4), Enemy(2, 0.7, 0.4) };
        LockOn lockOn = LockedOnto(seen, 1);

        lockOn.Update(seen, new StickPosition(0.5, 0), Frame);

        Assert.Equal(1u, lockOn.Target!.Id);
    }

    [Fact]
    public void WhenTheEnemyFallsTheLockMovesToTheOneNearestWhereItWas()
    {
        SeenTarget[] seen = { Enemy(1, 0.5, 0.4), Enemy(2, 0.55, 0.45), Enemy(3, 0.9, 0.4) };
        LockOn lockOn = LockedOnto(seen, 1);
        SeenTarget[] after = { Enemy(2, 0.55, 0.45), Enemy(3, 0.9, 0.4) };

        Assert.Equal(LockChange.None, lockOn.Update(after, Still, TimeSpan.FromMilliseconds(100)));
        Assert.Equal(LockChange.Switched, lockOn.Update(after, Still, TimeSpan.FromMilliseconds(200)));
        Assert.Equal(2u, lockOn.Target!.Id);
    }

    [Fact]
    public void OneLateReportDoesNotDropTheLock()
    {
        SeenTarget[] seen = { Enemy(1, 0.5, 0.4), Enemy(2, 0.55, 0.45) };
        LockOn lockOn = LockedOnto(seen, 1);

        lockOn.Update(new[] { Enemy(2, 0.55, 0.45) }, Still, TimeSpan.FromMilliseconds(100));
        lockOn.Update(seen, Still, Frame);
        lockOn.Update(new[] { Enemy(2, 0.55, 0.45) }, Still, TimeSpan.FromMilliseconds(200));

        Assert.Equal(1u, lockOn.Target!.Id);
    }

    [Fact]
    public void WithNothingLeftTheLockIsLost()
    {
        LockOn lockOn = LockedOnto(new[] { Enemy(1, 0.5, 0.4) }, 1);

        lockOn.Update(Array.Empty<SeenTarget>(), Still, TimeSpan.FromMilliseconds(100));

        Assert.Equal(LockChange.Lost, lockOn.Update(Array.Empty<SeenTarget>(), Still, TimeSpan.FromMilliseconds(200)));
        Assert.Null(lockOn.Target);
    }

    [Fact]
    public void AnEnemyThatRunsBeyondReachBreaksTheLock()
    {
        LockOn lockOn = LockedOnto(new[] { Enemy(1, 0.5, 0.4) }, 1);

        Assert.Equal(LockChange.None, lockOn.Update(new[] { Enemy(1, 0.5, 0.3, distance: 30) }, Still, Frame));
        Assert.Equal(LockChange.Lost, lockOn.Update(new[] { Enemy(1, 0.5, 0.2, distance: 33) }, Still, Frame));
    }

    [Fact]
    public void AnEnemyLongOutOfViewBreaksTheLock()
    {
        LockOn lockOn = LockedOnto(new[] { Enemy(1, 0.5, 0.4) }, 1);
        SeenTarget[] hidden = { Enemy(1, 1.2, 0.4, onScreen: false) };

        Assert.Equal(LockChange.None, lockOn.Update(hidden, Still, TimeSpan.FromSeconds(2)));
        Assert.Equal(LockChange.Lost, lockOn.Update(hidden, Still, TimeSpan.FromSeconds(1)));
    }
}
