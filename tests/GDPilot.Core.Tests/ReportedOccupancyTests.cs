using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class ReportedOccupancyTests
{
    private static readonly GridSpec Bag = new(1044, 645, 32, 32, 8, 8);

    private static readonly LayoutReport Sword = new("Inventory.Bag2", 8, 8, new[] { new ItemRect(11, 0, 0, 1, 3) });

    [Fact]
    public void TheGamesReportIsPreferredToTheScreen()
    {
        Fallback screen = new();
        ReportedOccupancy source = new(screen);
        source.Accept(Sword);

        OccupancyMap? map = source.Read("Inventory.Bag2", Bag, null);

        Assert.Equal(CellState.Occupied, map![new GridCell(0, 2)]);
        Assert.Equal(0, screen.Reads);
    }

    [Fact]
    public void AGridTheGameHasNotDescribedIsReadFromTheScreen()
    {
        Fallback screen = new();
        ReportedOccupancy source = new(screen);
        source.Accept(Sword);

        source.Read("Inventory", Bag with { Columns = 12 }, null);

        Assert.Equal(1, screen.Reads);
    }

    [Fact]
    public void AReportThatDoesNotFitTheCalibratedGridIsNotTrusted()
    {
        Fallback screen = new();
        ReportedOccupancy source = new(screen);
        source.Accept(Sword with { Columns = 10 });

        source.Read("Inventory.Bag2", Bag, null);

        Assert.Equal(1, screen.Reads);
    }

    [Fact]
    public void ADisconnectFallsBackToTheScreen()
    {
        Fallback screen = new();
        ReportedOccupancy source = new(screen);
        source.Accept(Sword);

        source.Forget();
        source.Read("Inventory.Bag2", Bag, null);

        Assert.Equal(1, screen.Reads);
    }

    [Fact]
    public void WithoutReportsOrAScreenThereIsNothingToJumpBy()
    {
        Assert.Null(new ReportedOccupancy(null).Read("Inventory.Bag2", Bag, null));
    }

    private sealed class Fallback : IOccupancySource
    {
        public int Reads { get; private set; }

        public OccupancyMap? Read(string name, GridSpec grid, OccupancyMap? previous)
        {
            Reads++;
            return null;
        }
    }
}
