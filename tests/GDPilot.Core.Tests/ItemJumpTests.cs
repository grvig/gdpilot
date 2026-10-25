using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class ItemJumpTests
{
    private static readonly GridSpec Bag = new(100, 100, 32, 32, 6, 4);

    private static readonly GridReadiness Ready = new(true, "ready", new Dictionary<string, GridSpec> { ["Inventory"] = Bag });

    [Fact]
    public void ThePressStaysInItsRowWhileTheRowHasAnItem()
    {
        OccupancyMap map = Map(
            "#.....",
            "....#.",
            "...#..",
            "......");

        Assert.Equal(new GridCell(3, 2), map.NextItem(new GridCell(0, 2), GridDirection.Right));
    }

    [Fact]
    public void AnEmptyRowGivesWayToTheNearestItemBeyondIt()
    {
        OccupancyMap map = Map(
            "#.....",
            "......",
            ".....#",
            "..#...");

        Assert.Equal(new GridCell(2, 3), map.NextItem(new GridCell(0, 0), GridDirection.Down));
    }

    [Fact]
    public void NothingFurtherThatWayGivesNoTarget()
    {
        OccupancyMap map = Map(
            "#.....",
            "......",
            "......",
            "......");

        Assert.Null(map.NextItem(new GridCell(0, 0), GridDirection.Left));
        Assert.Null(map.NextItem(new GridCell(0, 0), GridDirection.Up));
    }

    [Fact]
    public void TheFirstItemIsFoundRowByRow()
    {
        OccupancyMap map = Map(
            "......",
            "...#..",
            "#.....",
            "......");

        Assert.Equal(new GridCell(3, 1), map.FirstItem());
    }

    [Fact]
    public void TheNavigatorLandsOnTheFirstItemAndJumpsOverEmptyCells()
    {
        FakeSource source = new(Map(
            "......",
            ".#...#",
            "......",
            "......"));
        GridNavigator navigator = new(Ready, source);

        Assert.Equal(new PointerMoveTo(132, 132), navigator.Act(Mode.Inventory, "right"));
        Assert.Equal(new PointerMoveTo(260, 132), navigator.Act(Mode.Inventory, "right"));
        Assert.True(navigator.Jumping);
    }

    [Fact]
    public void ADirectionWithNothingInItLeavesTheCursorWhereItIs()
    {
        FakeSource source = new(Map(
            "#.....",
            "......",
            "......",
            "......"));
        GridNavigator navigator = new(Ready, source);
        navigator.Act(Mode.Inventory, "down");

        Assert.Equal(new PointerMoveTo(100, 100), navigator.Act(Mode.Inventory, "down"));
    }

    [Fact]
    public void ADoubtfulReadingFallsBackToSteppingCellByCell()
    {
        FakeSource source = new(Map(
            "??????",
            "??????",
            "#.....",
            "......"));
        GridNavigator navigator = new(Ready, source);
        navigator.Act(Mode.Inventory, "right");

        Assert.Equal(new PointerMoveTo(132, 100), navigator.Act(Mode.Inventory, "right"));
        Assert.False(navigator.Jumping);
    }

    [Fact]
    public void EachReadingIsGivenTheLastTrustedOneForThatSurface()
    {
        OccupancyMap first = Map(
            "#.....",
            "......",
            "......",
            "......");
        FakeSource source = new(first);
        GridNavigator navigator = new(Ready, source);
        navigator.Act(Mode.Inventory, "right");

        navigator.Act(Mode.Inventory, "right");

        Assert.Same(first, source.LastPrevious);
    }

    [Fact]
    public void FallingBackIsShownFirstOnTheOverlay()
    {
        FakeSource source = new(Map(
            "??????",
            "??????",
            "......",
            "......"));
        GridNavigator navigator = new(Ready, source);
        ButtonLayers layers = BindingTable.Parse(new Dictionary<string, string> { ["DPadRight"] = "grid:right" }, 0.5);
        ModeController modes = new(new Dictionary<Mode, KeyCode>(), _ => layers, navigator);
        Assert.False(modes.GridFellBack);

        navigator.Act(Mode.Inventory, "right");

        Assert.True(modes.GridFellBack);
        Assert.Equal("Grid: stepping, screen unclear", OverlayState.Build(Mode.Inventory, SuspendReason.None, layers, modes.GridFellBack).Legend[0]);
    }

    [Fact]
    public void SteppingByChoiceIsNotFallingBack()
    {
        GridNavigator navigator = new(Ready);

        navigator.Act(Mode.Inventory, "right");

        Assert.False(navigator.FellBack);
    }

    /// <summary>A map drawn as rows of # for an item, . for empty and ? for unsure.</summary>
    private static OccupancyMap Map(params string[] rows)
    {
        CellState[,] states = new CellState[rows[0].Length, rows.Length];

        for (int row = 0; row < rows.Length; row++)
        {
            for (int column = 0; column < rows[row].Length; column++)
            {
                states[column, row] = State(rows[row][column]);
            }
        }

        return new OccupancyMap(states);
    }

    private static CellState State(char symbol)
    {
        switch (symbol)
        {
            case '#':
                return CellState.Occupied;
            case '.':
                return CellState.Empty;
            default:
                return CellState.Unsure;
        }
    }

    private sealed class FakeSource : IOccupancySource
    {
        private readonly OccupancyMap map;

        public FakeSource(OccupancyMap map)
        {
            this.map = map;
        }

        public OccupancyMap? LastPrevious { get; private set; }

        public OccupancyMap? Read(string name, GridSpec grid, OccupancyMap? previous)
        {
            LastPrevious = previous;
            return map;
        }
    }
}
