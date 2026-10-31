using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class OverlayStateTests
{
    private static readonly ButtonLayers Combat = BindingTable.Parse(
        new Dictionary<string, string>
        {
            ["A"] = "key:Space",
            ["RightBumper"] = "mouse:left",
            ["DPadUp"] = "panel:Inventory",
            ["Start"] = "back",
            ["LT+A"] = "key:1",
        },
        0.5);

    [Fact]
    public void TheLegendDescribesEveryBindingBaseLayerFirst()
    {
        OverlayState state = OverlayState.Build(Mode.Combat, SuspendReason.None, Combat);

        Assert.Equal(
            new[] { "A: Space", "RightBumper: Left click", "Start: Back", "DPadUp: Inventory", "LT+A: 1" },
            state.Legend);
    }

    [Fact]
    public void TheModeIsShownByName()
    {
        Assert.Equal("Inventory", OverlayState.Build(Mode.Inventory, SuspendReason.None, Combat).Mode);
    }

    [Fact]
    public void NothingSuspendedShowsNoSuspendedLine()
    {
        Assert.Null(OverlayState.Build(Mode.Combat, SuspendReason.None, Combat).Suspended);
    }

    [Theory]
    [InlineData(SuspendReason.FocusLost, "Suspended: game not in front")]
    [InlineData(SuspendReason.KillSwitch, "Suspended: kill switch")]
    [InlineData(SuspendReason.KillSwitch | SuspendReason.FocusLost, "Suspended: kill switch")]
    public void SuspensionSaysWhy(SuspendReason reasons, string expected)
    {
        Assert.Equal(expected, OverlayState.Build(Mode.Combat, reasons, Combat).Suspended);
    }

    [Fact]
    public void ALockSaysSoFirstWithTheEnemysLife()
    {
        OverlayState state = OverlayState.Build(Mode.Combat, SuspendReason.None, Combat, thirdPerson: true, locked: new SeenTarget(5, 0.5, 0.4, 6, 64, true));

        Assert.Equal("Locked on: 64% life; flick the right stick to switch", state.Legend[0]);
        Assert.DoesNotContain(state.Legend, line => line.StartsWith("Camera:"));
    }

    [Fact]
    public void TheMarkerSitsWhereTheCursorFollowsTheEnemy()
    {
        LockMarker? marker = LockMarker.For(new SeenTarget(5, 0.5, 0.25, 6, 64, true), (1060, 320));

        Assert.Equal(new LockMarker(1060, 320, 64), marker);
    }

    [Fact]
    public void ThereIsNoMarkerForAnEnemyOutOfViewOrWithoutAPoint()
    {
        Assert.Null(LockMarker.For(new SeenTarget(5, 1.3, 0.25, 6, 64, false), (1060, 320)));
        Assert.Null(LockMarker.For(new SeenTarget(5, 0.5, 0.25, 6, 64, true), null));
        Assert.Null(LockMarker.For(null, (1060, 320)));
    }

    [Fact]
    public void AwayFromTheGameNeitherTheMarkerNorTheWheelIsDrawn()
    {
        OverlayState drawn = OverlayState.Build(Mode.Combat, SuspendReason.FocusLost, Combat) with
        {
            Marker = new LockMarker(960, 400, 80),
            Wheel = new WheelView(new[] { "1" }, null),
        };

        Assert.Null(drawn.Floating().Marker);
        Assert.Null(drawn.Floating().Wheel);
        Assert.NotNull(OverlayState.Build(Mode.Combat, SuspendReason.None, Combat).Floating());
    }

    [Fact]
    public void InTheGameTheMarkerStays()
    {
        OverlayState drawn = OverlayState.Build(Mode.Combat, SuspendReason.None, Combat) with { Marker = new LockMarker(960, 400, 80) };

        Assert.NotNull(drawn.Floating().Marker);
    }

    [Fact]
    public void SteeringIsSaidSoTheEightWayFallbackIsNoticed()
    {
        Assert.Contains("Left stick: walks in any direction", OverlayState.Build(Mode.Combat, SuspendReason.None, Combat, steering: true).Legend);
        Assert.DoesNotContain("Left stick: walks in any direction", OverlayState.Build(Mode.Combat, SuspendReason.None, Combat).Legend);
    }
}
