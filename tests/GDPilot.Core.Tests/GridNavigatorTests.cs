using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class GridNavigatorTests
{
    // The bag grids measured from tests/fixtures/screens/inventory.png.
    private static readonly GridReadiness Ready = new(
        true,
        "ready",
        new Dictionary<string, GridSpec>
        {
            ["Inventory"] = new GridSpec(652, 645, 32, 32, 12, 8),
            ["Inventory.Bag2"] = new GridSpec(1044, 645, 32, 32, 8, 8),
        });

    [Fact]
    public void TheFirstPressLandsOnTheTopLeftCell()
    {
        GridNavigator navigator = new(Ready);

        Assert.Equal(new PointerMoveTo(652, 645), navigator.Act(Mode.Inventory, "right"));
    }

    [Fact]
    public void LaterPressesStepOneCellAndStopAtTheEdge()
    {
        GridNavigator navigator = new(Ready);
        navigator.Act(Mode.Inventory, "down");

        Assert.Equal(new PointerMoveTo(684, 645), navigator.Act(Mode.Inventory, "right"));
        Assert.Equal(new PointerMoveTo(684, 677), navigator.Act(Mode.Inventory, "down"));
        Assert.Equal(new PointerMoveTo(684, 645), navigator.Act(Mode.Inventory, "up"));
        Assert.Equal(new PointerMoveTo(684, 645), navigator.Act(Mode.Inventory, "up"));
    }

    [Fact]
    public void NextMovesToTheModesOtherSurface()
    {
        GridNavigator navigator = new(Ready);
        navigator.Act(Mode.Inventory, "right");

        Assert.Equal(new PointerMoveTo(1044, 645), navigator.Act(Mode.Inventory, "next"));
        Assert.Equal(new PointerMoveTo(652, 645), navigator.Act(Mode.Inventory, "next"));
    }

    [Fact]
    public void AModeWithoutAGridDoesNothingAndSaysSo()
    {
        GridNavigator navigator = new(Ready);

        Assert.Null(navigator.Act(Mode.Stash, "right"));
        Assert.Equal("No grid is calibrated for Stash.", navigator.Notice);
    }

    [Fact]
    public void AnUnreadyProfileRefusesWithItsReason()
    {
        GridReadiness mismatch = new(false, "Grids were calibrated at 1920x1080, UI scale 0.5, but the game runs 2560x1440. Recalibrate.", new Dictionary<string, GridSpec>());
        GridNavigator navigator = new(mismatch);

        Assert.Null(navigator.Act(Mode.Inventory, "right"));
        Assert.Contains("Recalibrate", navigator.Notice);
    }

    [Fact]
    public void ADpadPressInTheInventoryMovesTheCursorThroughTheModeController()
    {
        ButtonLayers inventory = BindingTable.Parse(new Dictionary<string, string> { ["DPadRight"] = "grid:right", ["DPadUp"] = "panel:Inventory" }, 0.5);
        ModeController modes = new(new Dictionary<Mode, KeyCode>(), _ => inventory, new GridNavigator(Ready));
        TimeSpan tick = TimeSpan.FromMilliseconds(10);

        modes.Tick(GamepadSnapshot.Rest with { Buttons = GamepadButtons.DPadUp }, tick);
        modes.Tick(GamepadSnapshot.Rest, tick);
        List<InputIntent> sent = modes.Tick(GamepadSnapshot.Rest with { Buttons = GamepadButtons.DPadRight }, tick);

        Assert.Equal(Mode.Inventory, modes.Current);
        Assert.Equal(new InputIntent[] { new PointerMoveTo(652, 645) }, sent);
    }
}
