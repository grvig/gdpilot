using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class GridSpecTests
{
    // Twelve columns and eight rows, 40 pixels apart, top-left cell centred on (520, 610).
    private static readonly GridSpec Bags = GridSpec.FromCorners(520, 610, 960, 890, 12, 8);

    [Fact]
    public void PitchIsTheSpanOverTheCountLessOne()
    {
        Assert.Equal(40, Bags.PitchX, 9);
        Assert.Equal(40, Bags.PitchY, 9);
        Assert.True(Bags.IsCalibrated);
    }

    [Fact]
    public void CellCentresFollowOriginAndPitch()
    {
        Assert.Equal((520, 610), Bags.Centre(new GridCell(0, 0)));
        Assert.Equal((600, 650), Bags.Centre(new GridCell(2, 1)));
        Assert.Equal((960, 890), Bags.Centre(new GridCell(11, 7)));
    }

    [Fact]
    public void FractionalPitchRoundsToTheNearestPixel()
    {
        GridSpec grid = new(100, 100, 37.5, 37.5, 10, 10);

        Assert.Equal((138, 138), grid.Centre(new GridCell(1, 1)));
    }

    [Theory]
    [InlineData(GridDirection.Right, 3, 2)]
    [InlineData(GridDirection.Left, 1, 2)]
    [InlineData(GridDirection.Down, 2, 3)]
    [InlineData(GridDirection.Up, 2, 1)]
    public void AStepMovesOneCell(GridDirection direction, int column, int row)
    {
        Assert.Equal(new GridCell(column, row), Bags.Step(new GridCell(2, 2), direction));
    }

    [Theory]
    [InlineData(GridDirection.Left, 0, 0)]
    [InlineData(GridDirection.Up, 0, 0)]
    public void StepsStopAtTheTopLeftEdges(GridDirection direction, int column, int row)
    {
        Assert.Equal(new GridCell(column, row), Bags.Step(new GridCell(0, 0), direction));
    }

    [Theory]
    [InlineData(GridDirection.Right)]
    [InlineData(GridDirection.Down)]
    public void StepsStopAtTheBottomRightEdges(GridDirection direction)
    {
        Assert.Equal(new GridCell(11, 7), Bags.Step(new GridCell(11, 7), direction));
    }

    [Fact]
    public void TheConfigurationsZeroPlaceholderIsNotCalibrated()
    {
        Assert.False(new GridSpec(0, 0, 0, 0, 0, 0).IsCalibrated);
    }

    [Theory]
    [InlineData(1, 8)]
    [InlineData(12, 1)]
    public void ASingleRowOrColumnNeedsItsCornersInLine(int columns, int rows)
    {
        Assert.Throws<ArgumentException>(() => GridSpec.FromCorners(520, 610, 960, 890, columns, rows));
    }

    [Fact]
    public void ATabStripIsOneRowWithLevelCorners()
    {
        GridSpec tabs = GridSpec.FromCorners(520, 300, 700, 300, 4, 1);

        Assert.Equal((640, 300), tabs.Centre(new GridCell(2, 0)));
        Assert.Equal(1, tabs.Rows);
    }

    [Fact]
    public void CornersTheWrongWayRoundAreRejected()
    {
        Assert.Throws<ArgumentException>(() => GridSpec.FromCorners(960, 890, 520, 610, 12, 8));
    }
}
