using GDPilot.Core;

namespace GDPilot.Core.Tests;

/// <summary>
/// Maps built from the game's own record of item rectangles, laid out like the
/// second bag on 2026-10-25: a three-cell sword down the first column, a 2 by 3
/// armour beside it, and a 2 by 2 tome below.
/// </summary>
public sealed class ItemLayoutTests
{
    private static readonly OccupancyMap Bag = OccupancyMap.FromItems(
        8,
        8,
        new[]
        {
            new ItemRect(11, 0, 0, 1, 3),
            new ItemRect(12, 1, 0, 2, 3),
            new ItemRect(13, 3, 0, 2, 1),
            new ItemRect(14, 1, 3, 2, 2),
        });

    [Fact]
    public void PressingDownFromATallItemLeavesItFromItsBottom()
    {
        // The tome starts a row below the sword's last cell; the armour beside the sword is not below it.
        Assert.Equal(new GridCell(1, 3), Bag.NextItem(new GridCell(0, 0), GridDirection.Down));
    }

    [Fact]
    public void NothingBelowATallItemMeansNoStopInsideIt()
    {
        OccupancyMap sword = OccupancyMap.FromItems(1, 4, new[] { new ItemRect(11, 0, 0, 1, 3) });

        Assert.Null(sword.NextItem(new GridCell(0, 0), GridDirection.Down));
    }

    [Fact]
    public void PressingRightLeavesAWideItemAndLandsOnTheNextOnesTopLeft()
    {
        Assert.Equal(new GridCell(1, 0), Bag.NextItem(new GridCell(0, 0), GridDirection.Right));
        Assert.Equal(new GridCell(3, 0), Bag.NextItem(new GridCell(1, 0), GridDirection.Right));
    }

    [Fact]
    public void ReachingAnItemFromBelowStillLandsOnItsTopLeft()
    {
        Assert.Equal(new GridCell(1, 0), Bag.NextItem(new GridCell(1, 3), GridDirection.Up));
    }

    [Fact]
    public void FromAnywhereInsideAnItemThePressLeavesIt()
    {
        Assert.Equal(new GridCell(1, 3), Bag.NextItem(new GridCell(2, 2), GridDirection.Down));
    }

    [Fact]
    public void EveryCellIsKnownSoTheMapIsAlwaysTrusted()
    {
        Assert.True(Bag.IsConfident);
        Assert.Equal(CellState.Occupied, Bag[new GridCell(0, 2)]);
        Assert.Equal(CellState.Empty, Bag[new GridCell(0, 3)]);
    }

    [Fact]
    public void AnItemReachingPastTheGridIsCutToFit()
    {
        OccupancyMap map = OccupancyMap.FromItems(2, 2, new[] { new ItemRect(5, 1, 1, 3, 3) });

        Assert.Equal(CellState.Occupied, map[new GridCell(1, 1)]);
        Assert.Equal(new GridCell(1, 1), map.FirstItem());
    }

    [Fact]
    public void CellsReadOneByOneStillStopOnEveryCell()
    {
        CellState[,] states = new CellState[1, 3];
        states[0, 0] = CellState.Occupied;
        states[0, 1] = CellState.Occupied;
        states[0, 2] = CellState.Occupied;
        OccupancyMap read = new(states);

        Assert.Equal(new GridCell(0, 1), read.NextItem(new GridCell(0, 0), GridDirection.Down));
    }
}
