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
}
