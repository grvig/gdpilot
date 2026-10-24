using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class StashTabTests
{
    private static readonly GridReadiness Ready = new(
        true,
        "ready",
        new Dictionary<string, GridSpec>
        {
            ["Stash"] = new GridSpec(500, 400, 32, 32, 10, 18),
            ["Stash.Tabs"] = new GridSpec(520, 300, 60, 1, 4, 1),
        });

    [Fact]
    public void NextTabMovesToItAndClicksIt()
    {
        GridNavigator navigator = new(Ready);

        List<InputIntent> sent = navigator.Tab(Mode.Stash, 1);

        Assert.Equal(new InputIntent[] { new PointerMoveTo(580, 300), new PointerDown(PointerButton.Left), new PointerUp(PointerButton.Left) }, sent);
    }

    [Fact]
    public void TabsWrapRoundInBothDirections()
    {
        GridNavigator navigator = new(Ready);

        Assert.Equal(new PointerMoveTo(700, 300), navigator.Tab(Mode.Stash, -1)[0]);
        Assert.Equal(new PointerMoveTo(520, 300), navigator.Tab(Mode.Stash, 1)[0]);
    }

    [Fact]
    public void ATabColumnIsSteppedDownwards()
    {
        GridReadiness column = new(
            true,
            "ready",
            new Dictionary<string, GridSpec> { ["Stash.Tabs"] = new GridSpec(473, 463, 1, 38, 1, 2) });
        GridNavigator navigator = new(column);

        Assert.Equal(new PointerMoveTo(473, 501), navigator.Tab(Mode.Stash, 1)[0]);
        Assert.Equal(new PointerMoveTo(473, 463), navigator.Tab(Mode.Stash, 1)[0]);
    }

    [Fact]
    public void TheTabStripIsNotOneOfTheGridsNextCyclesThrough()
    {
        GridNavigator navigator = new(Ready);
        navigator.Act(Mode.Stash, "right");

        Assert.Equal(new PointerMoveTo(500, 400), navigator.Act(Mode.Stash, "next"));
    }

    [Fact]
    public void AModeWithoutTabsDoesNothingAndSaysSo()
    {
        GridNavigator navigator = new(Ready);

        Assert.Empty(navigator.Tab(Mode.Inventory, 1));
        Assert.Equal("No tabs are calibrated for Inventory.", navigator.Notice);
    }

    [Fact]
    public void BumperBindingsReachTheTabsThroughTheModeController()
    {
        ButtonLayers stash = BindingTable.Parse(new Dictionary<string, string> { ["RightBumper"] = "tab:next", ["LeftBumper"] = "tab:previous", ["DPadDown"] = "panel:Stash" }, 0.5);
        ModeController modes = new(new Dictionary<Mode, KeyCode>(), _ => stash, new GridNavigator(Ready));
        TimeSpan tick = TimeSpan.FromMilliseconds(10);
        modes.Tick(GamepadSnapshot.Rest with { Buttons = GamepadButtons.DPadDown }, tick);
        modes.Tick(GamepadSnapshot.Rest, tick);

        List<InputIntent> sent = modes.Tick(GamepadSnapshot.Rest with { Buttons = GamepadButtons.RightBumper }, tick);

        Assert.Equal(Mode.Stash, modes.Current);
        Assert.Contains(new PointerMoveTo(580, 300), sent);
        Assert.Contains(new PointerDown(PointerButton.Left), sent);
    }

    [Fact]
    public void TabAndGridBindingsReadPlainlyInTheLegend()
    {
        ButtonLayers stash = BindingTable.Parse(new Dictionary<string, string> { ["RightBumper"] = "tab:next", ["DPadUp"] = "grid:up", ["Back"] = "grid:next" }, 0.5);

        IReadOnlyList<string> legend = OverlayState.Build(Mode.Stash, SuspendReason.None, stash).Legend;

        Assert.Contains("RightBumper: Next tab", legend);
        Assert.Contains("DPadUp: Cell up", legend);
        Assert.Contains("Back: Next grid", legend);
    }
}
