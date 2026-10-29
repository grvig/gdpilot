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
    public void TheMarkerSitsOverTheEnemyInScreenPixels()
    {
        LockMarker? marker = LockMarker.For(new SeenTarget(5, 0.5, 0.25, 6, 64, true), new ScreenRect(100, 50, 1920, 1080));

        Assert.Equal(new LockMarker(1060, 320, 64), marker);
    }

    [Fact]
    public void ThereIsNoMarkerForAnEnemyOutOfViewOrWithoutAGameWindow()
    {
        Assert.Null(LockMarker.For(new SeenTarget(5, 1.3, 0.25, 6, 64, false), new ScreenRect(0, 0, 1920, 1080)));
        Assert.Null(LockMarker.For(new SeenTarget(5, 0.5, 0.25, 6, 64, true), null));
        Assert.Null(LockMarker.For(null, new ScreenRect(0, 0, 1920, 1080)));
    }
}
