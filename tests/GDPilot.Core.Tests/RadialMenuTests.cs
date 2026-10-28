using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class RadialMenuTests
{
    private static readonly KeyCode[] Hotbar = { KeyCode.D1, KeyCode.D2, KeyCode.D3, KeyCode.D4 };

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(1, 0, 1)]
    [InlineData(0, -1, 2)]
    [InlineData(-1, 0, 3)]
    [InlineData(0.3, 0.95, 0)]
    public void SlotsRunClockwiseFromTheTop(double x, double y, int slot)
    {
        Assert.Equal(slot, new RadialMenu(Hotbar).SlotAt(new StickPosition(x, y)));
    }

    [Fact]
    public void FlickingAndLettingGoOfTheButtonCastsThePointedSlot()
    {
        RadialMenu wheel = new(Hotbar);
        wheel.Update(true, StickPosition.Centre);
        wheel.Update(true, new StickPosition(1, 0));

        Assert.Equal(KeyCode.D2, wheel.Update(false, StickPosition.Centre));
        Assert.False(wheel.Open);
    }

    [Fact]
    public void LettingTheStickReturnFirstKeepsTheChoice()
    {
        RadialMenu wheel = new(Hotbar);
        wheel.Update(true, new StickPosition(0, -1));
        wheel.Update(true, StickPosition.Centre);

        Assert.Equal(KeyCode.D3, wheel.Update(false, StickPosition.Centre));
    }

    [Fact]
    public void OpeningAndClosingWithoutPointingCastsNothing()
    {
        RadialMenu wheel = new(Hotbar);
        wheel.Update(true, new StickPosition(0.2, 0.2));

        Assert.Null(wheel.Update(false, StickPosition.Centre));
    }

    [Fact]
    public void EachOpeningStartsWithNothingChosen()
    {
        RadialMenu wheel = new(Hotbar);
        wheel.Update(true, new StickPosition(1, 0));
        wheel.Update(false, StickPosition.Centre);

        wheel.Update(true, StickPosition.Centre);

        Assert.Null(wheel.Selected);
    }

    private static readonly MappingSettings Settings = new(0.2, 0.2, 0.25, DirectionKeys.Wasd, new CursorSettings(900, 2.0, 2600));

    private static readonly ButtonLayers Combat = BindingTable.Parse(new Dictionary<string, string> { ["Back"] = "radial:hotbar", ["A"] = "key:Space" }, 0.5);

    [Fact]
    public void HoldingTheWheelButtonAndLettingGoTapsThePointedSlotsKey()
    {
        Mapper mapper = new(Settings, Combat) { Wheel = new RadialMenu(Hotbar) };
        TimeSpan tick = TimeSpan.FromMilliseconds(10);
        GamepadSnapshot holding = GamepadSnapshot.Rest with { Buttons = GamepadButtons.Back };
        List<InputIntent> sent = new();

        sent.AddRange(mapper.Tick(holding with { RightStick = new StickPosition(1, 0) }, tick));
        sent.AddRange(mapper.Tick(holding, tick));
        Assert.Empty(sent);

        for (int index = 0; index < 6; index++)
        {
            sent.AddRange(mapper.Tick(GamepadSnapshot.Rest, tick));
        }

        Assert.Equal(new InputIntent[] { new KeyDown(KeyCode.D2), new KeyUp(KeyCode.D2) }, sent);
    }

    [Fact]
    public void WhileTheWheelIsOpenTheRightStickMovesNoCursorAndFaceButtonsStillWork()
    {
        Mapper mapper = new(Settings, Combat) { Wheel = new RadialMenu(Hotbar) };

        List<InputIntent> sent = mapper.Tick(GamepadSnapshot.Rest with { Buttons = GamepadButtons.Back | GamepadButtons.A, RightStick = new StickPosition(1, 0) }, TimeSpan.FromMilliseconds(20));

        Assert.Equal(new InputIntent[] { new KeyDown(KeyCode.Space) }, sent);
    }

    [Fact]
    public void TheWheelReadsPlainlyInTheLegendAndTheConfigurationGivesTenSlots()
    {
        Assert.Contains("Back: Skill wheel", OverlayState.Build(Mode.Combat, SuspendReason.None, Combat).Legend);
        Assert.Equal(KeyCode.D0, GDPilotConfig.Parse("{}").Wheel()!.Slots[9]);
    }

    [Fact]
    public void TheOverlayIsGivenDigitsAndThePointedSlotOnlyWhileTheWheelIsOpen()
    {
        RadialMenu wheel = new(Hotbar);
        Assert.Null(wheel.View());

        wheel.Update(true, new StickPosition(-1, 0));

        Assert.Equal(new[] { "1", "2", "3", "4" }, wheel.View()!.Labels);
        Assert.Equal(3, wheel.View()!.Selected);
    }
}
