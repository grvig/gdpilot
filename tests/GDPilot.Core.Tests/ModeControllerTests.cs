using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class ModeControllerTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(10);

    private readonly ModeController controller;
    private readonly List<InputIntent> sent = new();

    public ModeControllerTests()
    {
        ButtonLayers layers = BindingTable.Parse(
            new Dictionary<string, string>
            {
                ["DPadUp"] = "panel:Inventory",
                ["DPadDown"] = "panel:Stash",
                ["B"] = "back",
            },
            0.5);
        controller = new ModeController(new Dictionary<Mode, KeyCode> { [Mode.Inventory] = KeyCode.I }, _ => layers);
    }

    [Fact]
    public void APanelBindingTapsItsKeyAndOpensTheMode()
    {
        Press(GamepadButtons.DPadUp);
        Run(GamepadSnapshot.Rest, 10);

        Assert.Equal(Mode.Inventory, controller.Current);
        Assert.Equal(new InputIntent[] { new KeyDown(KeyCode.I), new KeyUp(KeyCode.I) }, sent);
    }

    [Fact]
    public void ATapIsHeldLongEnoughForTheGameToSeeIt()
    {
        Press(GamepadButtons.DPadUp);
        Run(GamepadSnapshot.Rest, 2);

        Assert.Equal(new InputIntent[] { new KeyDown(KeyCode.I) }, sent);
    }

    [Fact]
    public void HoldingTheButtonActsOnlyOnce()
    {
        Run(Pressed(GamepadButtons.DPadUp), 20);

        Assert.Equal(Mode.Inventory, controller.Current);
        Assert.Single(sent, intent => intent is KeyDown);
    }

    [Fact]
    public void APanelWithoutAKeyOnlyDeclaresTheMode()
    {
        Press(GamepadButtons.DPadDown);
        Run(GamepadSnapshot.Rest, 10);

        Assert.Equal(Mode.Stash, controller.Current);
        Assert.Empty(sent);
    }

    [Fact]
    public void BackClosesThePanelAndForwardsEscape()
    {
        Press(GamepadButtons.DPadUp);
        Run(GamepadSnapshot.Rest, 10);
        sent.Clear();

        Press(GamepadButtons.B);
        Run(GamepadSnapshot.Rest, 10);

        Assert.Equal(Mode.Combat, controller.Current);
        Assert.Equal(new InputIntent[] { new KeyDown(KeyCode.Escape), new KeyUp(KeyCode.Escape) }, sent);
    }

    [Fact]
    public void BackFromCombatOpensTheMenu()
    {
        Press(GamepadButtons.B);

        Assert.Equal(Mode.Menu, controller.Current);
    }

    [Fact]
    public void PanicResyncSendsOneEscapePerPanelThenReturnsToCombat()
    {
        Press(GamepadButtons.DPadUp);
        Press(GamepadButtons.DPadDown);
        Run(GamepadSnapshot.Rest, 10);
        sent.Clear();

        Run(Pressed(GamepadButtons.Start | GamepadButtons.Back), 60);
        Run(GamepadSnapshot.Rest, 10);

        Assert.Equal(Mode.Combat, controller.Current);
        Assert.Equal(2, sent.Count(intent => intent == new KeyDown(KeyCode.Escape)));
        Assert.Equal(2, sent.Count(intent => intent == new KeyUp(KeyCode.Escape)));
    }

    [Fact]
    public void ABriefStartAndBackIsNotAPanic()
    {
        Press(GamepadButtons.DPadUp);
        Run(Pressed(GamepadButtons.Start | GamepadButtons.Back), 20);

        Assert.Equal(Mode.Inventory, controller.Current);
    }

    private void Press(GamepadButtons button)
    {
        Run(Pressed(button), 1);
        Run(GamepadSnapshot.Rest, 1);
    }

    private void Run(GamepadSnapshot snapshot, int ticks)
    {
        for (int tick = 0; tick < ticks; tick++)
        {
            sent.AddRange(controller.Tick(snapshot, Tick));
        }
    }

    private static GamepadSnapshot Pressed(GamepadButtons buttons)
    {
        return GamepadSnapshot.Rest with { Buttons = buttons };
    }
}
