using GDPilot.Core;

namespace GDPilot.Core.Tests;

/// <summary>What the face buttons do while the skill wheel is held and pointing at a slot.</summary>
public sealed class WheelButtonsTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(4);

    private static readonly ButtonLayers Layers = BindingTable.Parse(
        new Dictionary<string, string> { ["Back"] = "radial:hotbar", ["X"] = "key:2", ["B"] = "dodge:4", ["A"] = "interact:Space", ["LT+X"] = "key:5" },
        0.5);

    private static Mapper Wheeled()
    {
        KeyCode[] slots = { KeyCode.D1, KeyCode.D2, KeyCode.D3, KeyCode.D4, KeyCode.D5, KeyCode.D6, KeyCode.D7, KeyCode.D8, KeyCode.D9, KeyCode.D0 };
        return new Mapper(new GDPilotConfig().Mapping(), Layers) { Wheel = new RadialMenu(slots), HotbarSlot = key => (700 + (int)key, 1059) };
    }

    /// <summary>The wheel's button held and the stick pointing straight up, at the slot of key 1.</summary>
    private static GamepadSnapshot Pointing(GamepadButtons also = GamepadButtons.None, double trigger = 0)
    {
        return GamepadSnapshot.Rest with { Buttons = GamepadButtons.Back | also, RightStick = new StickPosition(0, 1), LeftTrigger = trigger };
    }

    private static void Hold(Mapper mapper, GamepadSnapshot snapshot)
    {
        mapper.Tick(snapshot, Tick);
        mapper.Tick(snapshot, Tick);
    }

    [Fact]
    public void ASkillsButtonExchangesItsSlotWithTheOnePointedAtAndCastsNothing()
    {
        Mapper mapper = Wheeled();
        Hold(mapper, Pointing());

        List<InputIntent> intents = mapper.Tick(Pointing(GamepadButtons.X), Tick);

        Assert.Contains(new GameCommandIntent(new SwapSlotsCommand(0, 1)), intents);
        Assert.DoesNotContain(intents, intent => intent is KeyDown);
    }

    [Fact]
    public void TheDodgeButtonExchangesItsSlotToo()
    {
        Mapper mapper = Wheeled();
        Hold(mapper, Pointing());

        Assert.Contains(new GameCommandIntent(new SwapSlotsCommand(0, 3)), mapper.Tick(Pointing(GamepadButtons.B), Tick));
    }

    [Fact]
    public void TheChordLayersSkillsExchangeToo()
    {
        Mapper mapper = Wheeled();
        Hold(mapper, Pointing());
        mapper.Tick(Pointing(GamepadButtons.None, 1), Tick);

        Assert.Contains(new GameCommandIntent(new SwapSlotsCommand(0, 4)), mapper.Tick(Pointing(GamepadButtons.X, 1), Tick));
    }

    [Fact]
    public void HoldingTheButtonExchangesOnlyOnce()
    {
        Mapper mapper = Wheeled();
        Hold(mapper, Pointing());
        mapper.Tick(Pointing(GamepadButtons.X), Tick);

        Assert.DoesNotContain(mapper.Tick(Pointing(GamepadButtons.X), Tick), intent => intent is GameCommandIntent { Command: SwapSlotsCommand });
    }

    [Fact]
    public void NotPointingAtASlotTheButtonDoesNothing()
    {
        Mapper mapper = Wheeled();
        GamepadSnapshot open = GamepadSnapshot.Rest with { Buttons = GamepadButtons.Back };
        Hold(mapper, open);

        List<InputIntent> intents = mapper.Tick(open with { Buttons = GamepadButtons.Back | GamepadButtons.X }, Tick);

        Assert.DoesNotContain(intents, intent => intent is KeyDown or GameCommandIntent { Command: SwapSlotsCommand });
    }

    [Fact]
    public void TheInteractButtonRestsOnThePointedSlotThenRightClicksItWithoutCasting()
    {
        Mapper mapper = Wheeled();
        Hold(mapper, Pointing());

        List<InputIntent> first = mapper.Tick(Pointing(GamepadButtons.A), Tick);
        Assert.Contains(new PointerMoveTo(700 + (int)KeyCode.D1, 1059), first);
        Assert.DoesNotContain(first, intent => intent is PointerDown);
        Assert.False(mapper.SlotListOpened);

        List<InputIntent> rest = new();

        for (int tick = 0; tick < 60 && !mapper.SlotListOpened; tick++)
        {
            rest.AddRange(mapper.Tick(Pointing(GamepadButtons.A), Tick));
        }

        Assert.True(mapper.SlotListOpened);
        Assert.Equal(new InputIntent[] { new PointerDown(PointerButton.Right), new PointerUp(PointerButton.Right) }, rest.Where(intent => intent is PointerDown or PointerUp).ToArray());
        Assert.False(mapper.Wheel!.Open);
    }

    [Fact]
    public void AStickStillLeaningDoesNotDragTheCursorOffTheList()
    {
        Mapper mapper = Wheeled();
        mapper.InWorld = false;
        Hold(mapper, Pointing());
        mapper.Tick(Pointing(GamepadButtons.A), Tick);

        for (int tick = 0; tick < 60 && !mapper.SlotListOpened; tick++)
        {
            mapper.Tick(Pointing(GamepadButtons.A), Tick);
        }

        mapper.Buttons = BindingTable.Parse(new Dictionary<string, string>(), 0.5);
        GamepadSnapshot leaning = GamepadSnapshot.Rest with { RightStick = new StickPosition(0, 1) };

        Assert.DoesNotContain(mapper.Tick(leaning, Tick), intent => intent is PointerMove);

        mapper.Tick(GamepadSnapshot.Rest, Tick);

        Assert.Contains(mapper.Tick(leaning, TimeSpan.FromMilliseconds(50)), intent => intent is PointerMove);
    }

    [Fact]
    public void WithTheWheelShutASkillsButtonIsItsSkillAsEver()
    {
        Mapper mapper = Wheeled();

        Assert.Contains(new KeyDown(KeyCode.D2), mapper.Tick(GamepadSnapshot.Rest with { Buttons = GamepadButtons.X }, Tick));
    }

    [Fact]
    public void WithTheTriggerDownTheWheelsButtonIsItsChordsAndTheWheelStaysShut()
    {
        ButtonLayers layers = BindingTable.Parse(new Dictionary<string, string> { ["Back"] = "radial:hotbar", ["LT+Back"] = "panel:Achievements" }, 0.5);
        KeyCode[] slots = { KeyCode.D1, KeyCode.D2 };
        Mapper mapper = new(new GDPilotConfig().Mapping(), layers) { Wheel = new RadialMenu(slots) };

        mapper.Tick(GamepadSnapshot.Rest with { Buttons = GamepadButtons.Back, LeftTrigger = 1 }, Tick);

        Assert.False(mapper.Wheel!.Open);
    }
}
