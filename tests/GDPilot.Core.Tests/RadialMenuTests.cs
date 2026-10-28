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
}
