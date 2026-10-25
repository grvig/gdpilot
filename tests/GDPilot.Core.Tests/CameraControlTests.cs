using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class CameraControlTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(4);

    private static readonly MappingSettings Settings = new(0.2, 0.2, 0.25, DirectionKeys.Wasd, new CursorSettings(900, 2.0, 2600), 2.5, 0.6);

    private static readonly ButtonLayers NoButtons = BindingTable.Parse(new Dictionary<string, string>(), 0.5);

    private static GamepadSnapshot Right(double x)
    {
        return GamepadSnapshot.Rest with { RightStick = new StickPosition(x, 0) };
    }

    [Fact]
    public void InThirdPersonTheRightStickTurnsTheCameraInsteadOfTheCursor()
    {
        Mapper mapper = new(Settings, NoButtons) { Looking = true };

        List<InputIntent> sent = mapper.Tick(Right(1), Tick);

        Assert.Equal(new InputIntent[] { new GameCommandIntent(new LookCommand(2.5, 0)) }, sent);
    }

    [Fact]
    public void PushingTheRightStickUpZoomsInAndDownZoomsOut()
    {
        Mapper mapper = new(Settings, NoButtons) { Looking = true };

        Assert.Equal(new InputIntent[] { new GameCommandIntent(new LookCommand(0, 0.6)) }, mapper.Tick(GamepadSnapshot.Rest with { RightStick = new StickPosition(0, 1) }, Tick));
        Assert.Equal(new InputIntent[] { new GameCommandIntent(new LookCommand(0, -0.6)) }, mapper.Tick(GamepadSnapshot.Rest with { RightStick = new StickPosition(0, -1) }, Tick));
    }

    [Fact]
    public void WalkingSidewaysTurnsTheCameraTowardTheWalkWhenFollowingIsOn()
    {
        Mapper mapper = new(Settings with { FollowRadiansPerSecond = 1.2 }, NoButtons) { Looking = true };

        List<InputIntent> sent = mapper.Tick(GamepadSnapshot.Rest with { LeftStick = new StickPosition(1, 0) }, Tick);

        Assert.Contains(new GameCommandIntent(new LookCommand(1.2, 0)), sent);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(0, -1)]
    public void WalkingStraightOnOrBackingOffTurnsNothing(double x, double y)
    {
        Mapper mapper = new(Settings with { FollowRadiansPerSecond = 1.2 }, NoButtons) { Looking = true };

        List<InputIntent> sent = mapper.Tick(GamepadSnapshot.Rest with { LeftStick = new StickPosition(x, y) }, Tick);

        Assert.DoesNotContain(sent, intent => intent is GameCommandIntent);
    }

    [Fact]
    public void TheRightStickOverridesFollowing()
    {
        Mapper mapper = new(Settings with { FollowRadiansPerSecond = 1.2 }, NoButtons) { Looking = true };

        List<InputIntent> sent = mapper.Tick(GamepadSnapshot.Rest with { LeftStick = new StickPosition(1, 0), RightStick = new StickPosition(-1, 0) }, Tick);

        Assert.Contains(new GameCommandIntent(new LookCommand(-2.5, 0)), sent);
    }

    [Fact]
    public void FollowingIsOffUnlessSet()
    {
        Mapper mapper = new(Settings, NoButtons) { Looking = true };

        List<InputIntent> sent = mapper.Tick(GamepadSnapshot.Rest with { LeftStick = new StickPosition(1, 0) }, Tick);

        Assert.DoesNotContain(sent, intent => intent is GameCommandIntent);
    }

    [Fact]
    public void AHeldStickSendsItsRateOnceNotEveryTick()
    {
        Mapper mapper = new(Settings, NoButtons) { Looking = true };
        mapper.Tick(Right(1), Tick);

        Assert.Empty(mapper.Tick(Right(1), Tick));
        Assert.Equal(new InputIntent[] { new GameCommandIntent(new LookCommand(0, 0)) }, mapper.Tick(Right(0), Tick));
    }

    [Fact]
    public void InTheGamesOwnCameraTheStickStillMovesTheCursor()
    {
        Mapper mapper = new(Settings, NoButtons);

        List<InputIntent> sent = mapper.Tick(Right(1), TimeSpan.FromMilliseconds(20));

        Assert.IsType<PointerMove>(Assert.Single(sent));
    }

    [Fact]
    public void LeavingThirdPersonMidTurnStopsTheCamera()
    {
        Mapper mapper = new(Settings, NoButtons) { Looking = true };
        mapper.Tick(Right(1), Tick);

        mapper.Looking = false;
        List<InputIntent> sent = mapper.Tick(Right(1), TimeSpan.FromMilliseconds(20));

        Assert.Equal(new GameCommandIntent(new LookCommand(0, 0)), sent[0]);
        Assert.IsType<PointerMove>(sent[1]);
    }

    [Fact]
    public void ACameraToggleBindingAsksTheGameToSwitchAndReadsPlainly()
    {
        ButtonLayers combat = BindingTable.Parse(new Dictionary<string, string> { ["RightStick"] = "camera:toggle" }, 0.5);
        ModeController modes = new(new Dictionary<Mode, KeyCode>(), _ => combat);

        List<InputIntent> sent = modes.Tick(GamepadSnapshot.Rest with { Buttons = GamepadButtons.RightStick }, Tick);

        Assert.Contains(new GameCommandIntent(new ToggleCameraCommand()), sent);
        Assert.Contains("RightStick: Switch camera", OverlayState.Build(Mode.Combat, SuspendReason.None, combat).Legend);
    }

    [Fact]
    public void EnteringThirdPersonParksTheCursorAtTheAimPointOnce()
    {
        Mapper mapper = new(Settings, NoButtons) { Looking = true, AimPoint = (960, 410) };

        Assert.Equal(new InputIntent[] { new PointerMoveTo(960, 410) }, mapper.Tick(GamepadSnapshot.Rest, Tick));
        Assert.Empty(mapper.Tick(GamepadSnapshot.Rest, Tick));

        mapper.Looking = false;
        mapper.Tick(GamepadSnapshot.Rest, Tick);
        mapper.Looking = true;

        Assert.Equal(new InputIntent[] { new PointerMoveTo(960, 410) }, mapper.Tick(GamepadSnapshot.Rest, Tick));
    }

    [Fact]
    public void TheOverlaySaysWhenTheRightStickDrivesTheCamera()
    {
        Assert.Equal("Camera: third person; right stick turns and zooms", OverlayState.Build(Mode.Combat, SuspendReason.None, NoButtons, thirdPerson: true).Legend[0]);
        Assert.Empty(OverlayState.Build(Mode.Combat, SuspendReason.None, NoButtons).Legend);
    }

    [Fact]
    public void WithATiltSpeedTheStickUpTiltsAndTheTriggerMakesItZoom()
    {
        Mapper mapper = new(Settings with { TiltDegreesPerSecond = 60 }, NoButtons) { Looking = true };
        GamepadSnapshot up = GamepadSnapshot.Rest with { RightStick = new StickPosition(0, 1) };

        Assert.Equal(new InputIntent[] { new GameCommandIntent(new LookCommand(0, 0, 60)) }, mapper.Tick(up, Tick));
        Assert.Equal(new InputIntent[] { new GameCommandIntent(new LookCommand(0, 0.6, 0)) }, mapper.Tick(up with { LeftTrigger = 1 }, Tick));
    }

    [Fact]
    public void ATiltIsCarriedOnTheLookLineOnlyWhenThereIsOne()
    {
        Assert.Equal("""{"protocol":1,"type":"look","yaw":0,"zoom":0,"tilt":-30}""", new LookCommand(0, 0, -30).Write());
        Assert.Equal(new LookCommand(0, 0, -30), GameCommands.Parse(new LookCommand(0, 0, -30).Write()));
        Assert.DoesNotContain("tilt", new LookCommand(1, 0).Write());
    }
}
