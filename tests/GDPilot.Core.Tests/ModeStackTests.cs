using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class ModeStackTests
{
    private readonly ModeStack stack = new();

    [Fact]
    public void ItStartsInCombatWithNothingOpen()
    {
        Assert.Equal(Mode.Combat, stack.Current);
        Assert.Equal(0, stack.Depth);
    }

    [Fact]
    public void OpeningAPanelMakesItCurrent()
    {
        stack.Toggle(Mode.Inventory);

        Assert.Equal(Mode.Inventory, stack.Current);
        Assert.Equal(1, stack.Depth);
    }

    [Fact]
    public void PressingAnOpenPanelsKeyClosesIt()
    {
        stack.Toggle(Mode.Inventory);

        stack.Toggle(Mode.Inventory);

        Assert.Equal(Mode.Combat, stack.Current);
    }

    [Fact]
    public void ClosingAPanelBelowTheTopLeavesTheTopAlone()
    {
        stack.Toggle(Mode.Inventory);
        stack.Toggle(Mode.WorldMap);

        stack.Toggle(Mode.Inventory);

        Assert.Equal(Mode.WorldMap, stack.Current);
        Assert.Equal(1, stack.Depth);
    }

    [Fact]
    public void EscapeClosesOneLevelAtATime()
    {
        stack.Toggle(Mode.Character);
        stack.Toggle(Mode.Skills);

        Assert.True(stack.Pop());
        Assert.Equal(Mode.Character, stack.Current);
        Assert.True(stack.Pop());
        Assert.Equal(Mode.Combat, stack.Current);
    }

    [Fact]
    public void EscapeInCombatHasNothingToClose()
    {
        Assert.False(stack.Pop());
        Assert.Equal(Mode.Combat, stack.Current);
    }

    [Fact]
    public void ClearingReturnsToCombatFromAnyDepth()
    {
        stack.Toggle(Mode.Inventory);
        stack.Toggle(Mode.Stash);
        stack.Toggle(Mode.Menu);

        stack.ClearToCombat();

        Assert.Equal(Mode.Combat, stack.Current);
        Assert.Equal(0, stack.Depth);
    }

    [Theory]
    [InlineData(Mode.Combat)]
    [InlineData(Mode.Suspended)]
    public void CombatAndSuspendedAreNotPanels(Mode mode)
    {
        Assert.Throws<ArgumentException>(() => stack.Toggle(mode));
    }
}
