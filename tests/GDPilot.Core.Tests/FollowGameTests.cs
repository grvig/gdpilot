using GDPilot.Core;

namespace GDPilot.Core.Tests;

/// <summary>The tracked mode following the panels the game itself reports.</summary>
public sealed class FollowGameTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(10);

    private static readonly ButtonLayers Layers = BindingTable.Parse(
        new Dictionary<string, string> { ["DPadUp"] = "panel:Inventory", ["LT+DPadDown"] = "panel:Stash" },
        0.5);

    private static ModeController Controller()
    {
        return new ModeController(new Dictionary<Mode, KeyCode> { [Mode.Inventory] = KeyCode.I }, _ => Layers);
    }

    private static void Wait(ModeController modes, TimeSpan time)
    {
        for (TimeSpan waited = TimeSpan.Zero; waited < time; waited += Tick)
        {
            modes.Tick(GamepadSnapshot.Rest, Tick);
        }
    }

    [Fact]
    public void APanelClosedWithTheMouseReturnsTheToolToCombat()
    {
        ModeController modes = Controller();
        modes.Tick(GamepadSnapshot.Rest with { Buttons = GamepadButtons.DPadUp }, Tick);
        Wait(modes, TimeSpan.FromMilliseconds(400));
        Assert.Equal(Mode.Inventory, modes.Current);

        modes.Follow(Array.Empty<string>());

        Assert.Equal(Mode.Combat, modes.Current);
    }

    [Fact]
    public void APanelTheGameOpenedByItselfIsFollowed()
    {
        ModeController modes = Controller();

        modes.Follow(new[] { "Skills" });

        Assert.Equal(Mode.Skills, modes.Current);
    }

    [Fact]
    public void AReportArrivingRightAfterATapIsNotBelieved()
    {
        ModeController modes = Controller();
        modes.Tick(GamepadSnapshot.Rest with { Buttons = GamepadButtons.DPadUp }, Tick);
        Wait(modes, TimeSpan.FromMilliseconds(100));

        modes.Follow(Array.Empty<string>());

        Assert.Equal(Mode.Inventory, modes.Current);
    }

    [Fact]
    public void TheStashKeepsItsModeThoughTheGameCallsItTheInventory()
    {
        ModeController modes = Controller();
        modes.Tick(GamepadSnapshot.Rest with { Buttons = GamepadButtons.DPadDown, LeftTrigger = 1 }, Tick);
        Wait(modes, TimeSpan.FromMilliseconds(400));

        modes.Follow(new[] { "Inventory" });

        Assert.Equal(Mode.Stash, modes.Current);
    }

    [Fact]
    public void TheMenuOverEverythingElseIsWhatTheButtonsDrive()
    {
        ModeController modes = Controller();

        modes.Follow(new[] { "Inventory", "Menu" });

        Assert.Equal(Mode.Menu, modes.Current);
        Assert.Equal(1, modes.Depth);
    }

    [Fact]
    public void NamesThisVersionDoesNotKnowAreIgnored()
    {
        ModeController modes = Controller();

        modes.Follow(new[] { "Smithing" });

        Assert.Equal(Mode.Combat, modes.Current);
    }
}
