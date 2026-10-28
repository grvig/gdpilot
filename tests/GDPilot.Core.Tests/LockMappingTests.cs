using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class LockMappingTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(4);

    private static readonly MappingSettings Settings = new(0.2, 0.2, 0.25, DirectionKeys.Wasd, new CursorSettings(900, 2.0, 2600), 2.5, 0.6);

    private static readonly ButtonLayers Combat = BindingTable.Parse(new Dictionary<string, string> { ["RightStick"] = "lock:toggle", ["LT+RightStick"] = "camera:toggle" }, 0.5);

    private static readonly GamepadSnapshot Press = GamepadSnapshot.Rest with { Buttons = GamepadButtons.RightStick };

    private static readonly SeenTarget Near = new(11, 0.5, 0.4, 6, 100, true);
    private static readonly SeenTarget Right = new(12, 0.7, 0.4, 7, 100, true);

    private static Mapper Locking()
    {
        return new Mapper(Settings, Combat)
        {
            Lock = new LockOn(new LockSettings()),
            Seen = new[] { Near, Right },
            GameBounds = new ScreenRect(0, 0, 1920, 1080),
            Looking = true,
            AimPoint = (960, 410),
        };
    }

    [Fact]
    public void APressLocksOnTellsTheGameAndPutsTheCursorOnTheEnemy()
    {
        Mapper mapper = Locking();

        List<InputIntent> sent = mapper.Tick(Press, Tick);

        Assert.Equal(LockChange.Locked, mapper.LockChanged);
        Assert.Contains(new GameCommandIntent(new LockCommand(11)), sent);
        Assert.Contains(new PointerMoveTo(960, 432), sent);
    }

    [Fact]
    public void HoldingThePressDoesNotLetGo()
    {
        Mapper mapper = Locking();
        mapper.Tick(Press, Tick);

        mapper.Tick(Press, Tick);

        Assert.Equal(11u, mapper.Lock!.Target!.Id);
    }

    [Fact]
    public void WhileLockedTheRightStickFlicksInsteadOfTurning()
    {
        Mapper mapper = Locking();
        mapper.Tick(Press, Tick);
        mapper.Tick(GamepadSnapshot.Rest, Tick);

        List<InputIntent> sent = mapper.Tick(GamepadSnapshot.Rest with { RightStick = new StickPosition(1, 0) }, Tick);

        Assert.Equal(LockChange.Switched, mapper.LockChanged);
        Assert.Contains(new GameCommandIntent(new LockCommand(12)), sent);
        Assert.Contains(new PointerMoveTo(1344, 432), sent);
        Assert.DoesNotContain(sent, intent => intent is GameCommandIntent { Command: LookCommand { YawPerSecond: not 0.0 } });
    }

    [Fact]
    public void TheCursorFollowsAMovingEnemyButNotAStillOne()
    {
        Mapper mapper = Locking();
        mapper.Tick(Press, Tick);

        Assert.DoesNotContain(mapper.Tick(GamepadSnapshot.Rest, Tick), intent => intent is PointerMoveTo);

        mapper.Seen = new[] { Near with { X = 0.55 } };
        Assert.Contains(mapper.Tick(GamepadSnapshot.Rest, Tick), intent => intent is PointerMoveTo { X: >= 1056, Y: 432 });
    }

    [Fact]
    public void ASecondPressLetsGoAndTheCursorReturnsToTheAimPoint()
    {
        Mapper mapper = Locking();
        mapper.Tick(Press, Tick);
        mapper.Tick(GamepadSnapshot.Rest, Tick);

        List<InputIntent> sent = mapper.Tick(Press, Tick);

        Assert.Equal(LockChange.Released, mapper.LockChanged);
        Assert.Contains(new GameCommandIntent(new LockCommand(0)), sent);
        Assert.Contains(new PointerMoveTo(960, 410), sent);
    }

    [Fact]
    public void TheTriggerLayerSwitchesTheCameraInsteadOfLocking()
    {
        Mapper mapper = Locking();

        mapper.Tick(Press with { LeftTrigger = 1 }, Tick);

        Assert.Null(mapper.Lock!.Target);
    }

    [Fact]
    public void OpeningAPanelLetsGo()
    {
        Mapper mapper = Locking();
        mapper.Tick(Press, Tick);
        mapper.InWorld = false;

        List<InputIntent> sent = mapper.Tick(GamepadSnapshot.Rest, Tick);

        Assert.Null(mapper.Lock!.Target);
        Assert.Contains(new GameCommandIntent(new LockCommand(0)), sent);
    }

    [Fact]
    public void BetweenReportsTheCursorGlidesOnAtTheEnemysSpeed()
    {
        Mapper mapper = Locking();
        mapper.Tick(Press, Tick);
        mapper.Tick(GamepadSnapshot.Rest, TimeSpan.FromMilliseconds(32));
        mapper.Seen = new[] { Near with { X = 0.51 } };
        mapper.Tick(GamepadSnapshot.Rest, Tick);

        mapper.Tick(GamepadSnapshot.Rest, TimeSpan.FromMilliseconds(16));

        // 0.51, carried on a further 20 ms at 0.01 per 36 ms.
        Assert.Equal((990, 432), mapper.LockPoint);
    }

    [Fact]
    public void APressWithNothingToLockRecentresTheCameraInThirdPerson()
    {
        Mapper mapper = Locking();
        mapper.Seen = Array.Empty<SeenTarget>();

        Assert.Contains(new GameCommandIntent(new RecenterCommand()), mapper.Tick(Press, Tick));
    }

    [Fact]
    public void InTheGamesOwnCameraThereIsNothingToRecentre()
    {
        Mapper mapper = Locking();
        mapper.Seen = Array.Empty<SeenTarget>();
        mapper.Looking = false;

        Assert.DoesNotContain(new GameCommandIntent(new RecenterCommand()), mapper.Tick(Press, Tick));
    }

    [Fact]
    public void SoftAimDrawsTheCursorOntoAnEnemyNearTheAimPoint()
    {
        Mapper mapper = Locking();
        mapper.SoftAim = new SoftAim();
        mapper.Seen = new[] { new SeenTarget(21, 0.52, 0.4, 6, 100, true) };

        Assert.Contains(new PointerMoveTo(998, 432), mapper.Tick(GamepadSnapshot.Rest, Tick));
    }

    [Fact]
    public void WhenTheSoftTargetMovesAwayTheCursorReturnsToTheAimPoint()
    {
        Mapper mapper = Locking();
        mapper.SoftAim = new SoftAim();
        mapper.Seen = new[] { new SeenTarget(21, 0.52, 0.4, 6, 100, true) };
        mapper.Tick(GamepadSnapshot.Rest, Tick);

        mapper.Seen = new[] { new SeenTarget(21, 0.9, 0.4, 6, 100, true) };

        Assert.Contains(new PointerMoveTo(960, 410), mapper.Tick(GamepadSnapshot.Rest, Tick));
    }

    [Fact]
    public void WithoutSoftAimTheCursorWaitsAtTheAimPoint()
    {
        Mapper mapper = Locking();
        mapper.Seen = new[] { new SeenTarget(21, 0.52, 0.4, 6, 100, true) };

        Assert.Contains(new PointerMoveTo(960, 410), mapper.Tick(GamepadSnapshot.Rest, Tick));
    }
}
