using GDPilot.Core;

namespace GDPilot.Core.Tests;

/// <summary>
/// Synthetic panels drawn the way the game draws them: near-black cells inside
/// bright lines, with items as tinted boxes over some of them.
/// </summary>
public sealed class OccupancyMapTests
{
    private static readonly GridSpec Bag = new(116, 116, 32, 32, 4, 3);

    [Fact]
    public void ColouredAndGreyItemsAreFoundAndDarkCellsAreEmpty()
    {
        Dictionary<GridCell, (byte, byte, byte)> items = new()
        {
            [new GridCell(1, 0)] = (40, 90, 30),
            [new GridCell(3, 2)] = (22, 22, 22),
        };

        OccupancyMap map = OccupancyMap.Read(Bag, Panel(Bag, items, 6));

        Assert.Equal(CellState.Occupied, map[new GridCell(1, 0)]);
        Assert.Equal(CellState.Occupied, map[new GridCell(3, 2)]);
        Assert.Equal(CellState.Empty, map[new GridCell(0, 0)]);
        Assert.Equal(CellState.Empty, map[new GridCell(2, 1)]);
        Assert.True(map.IsConfident);
    }

    [Fact]
    public void TheBrightLinesBetweenCellsAreNeverMistakenForContent()
    {
        OccupancyMap map = OccupancyMap.Read(Bag, Panel(Bag, new Dictionary<GridCell, (byte, byte, byte)>(), 11));

        for (int column = 0; column < Bag.Columns; column++)
        {
            for (int row = 0; row < Bag.Rows; row++)
            {
                Assert.Equal(CellState.Empty, map[new GridCell(column, row)]);
            }
        }
    }

    [Fact]
    public void LargerCellsAreSampledInProportion()
    {
        GridSpec large = new(124, 124, 48, 48, 3, 2);
        Dictionary<GridCell, (byte, byte, byte)> items = new() { [new GridCell(2, 1)] = (120, 60, 160) };

        OccupancyMap map = OccupancyMap.Read(large, Panel(large, items, 6));

        Assert.Equal(CellState.Occupied, map[new GridCell(2, 1)]);
        Assert.Equal(CellState.Empty, map[new GridCell(1, 1)]);
    }

    [Theory]
    [InlineData(6, 0, CellState.Empty)]
    [InlineData(11.3, 0.5, CellState.Empty)]
    [InlineData(13, 0, CellState.Unsure)]
    [InlineData(15.3, 0, CellState.Occupied)]
    [InlineData(9, 11, CellState.Occupied)]
    public void ACellIsJudgedByBrightnessAndColour(double brightness, double colour, CellState expected)
    {
        Assert.Equal(expected, new CellLook(brightness, colour).State);
    }

    [Fact]
    public void ManyUnsureCellsMakeTheWholeReadingDoubtful()
    {
        OccupancyMap map = OccupancyMap.Read(Bag, Panel(Bag, new Dictionary<GridCell, (byte, byte, byte)>(), 13));

        Assert.False(map.IsConfident);
    }

    /// <summary>A panel with bright lines every pitch, dark cells of the given level, and solid items.</summary>
    private static Func<int, int, (byte, byte, byte)> Panel(GridSpec grid, Dictionary<GridCell, (byte, byte, byte)> items, byte empty)
    {
        return (x, y) =>
        {
            double left = grid.OriginX - grid.PitchX / 2;
            double top = grid.OriginY - grid.PitchY / 2;
            int column = (int)Math.Floor((x - left) / grid.PitchX);
            int row = (int)Math.Floor((y - top) / grid.PitchY);
            double insideX = (x - left) - column * grid.PitchX;
            double insideY = (y - top) - row * grid.PitchY;

            if (insideX < 2 || insideY < 2)
            {
                return (150, 140, 110);
            }

            if (items.TryGetValue(new GridCell(column, row), out (byte, byte, byte) item))
            {
                return item;
            }

            return (empty, empty, empty);
        };
    }
}
