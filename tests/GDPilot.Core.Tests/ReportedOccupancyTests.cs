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

    [Fact]
    public void ASackNotYetLaidOutIsNotBelieved()
    {
        ReportedOccupancy layouts = new(null);
        layouts.Accept(new LayoutReport("Vendor#1", 10, 18, new[] { new ItemRect(1, 0, 0, 0, 0) }));

        Assert.Null(layouts.Read("Vendor#1", new GridSpec(483, 323, 32, 32, 10, 18), null));
    }

    [Fact]
    public void AnUnplacedSackIsParsedSoItReplacesTheLastTradersGoods()
    {
        ReportedOccupancy layouts = new(null);
        layouts.Accept(new LayoutReport("Vendor#1", 10, 18, new[] { new ItemRect(1, 0, 0, 1, 1) }));

        string line = """{"protocol":1,"type":"layout","grid":"Vendor#1","columns":10,"rows":18,"items":[{"id":9,"column":0,"row":0,"width":0,"height":0}]}""";
        layouts.Accept((LayoutReport)GameReport.Parse(line)!);

        Assert.Null(layouts.Read("Vendor#1", new GridSpec(483, 323, 32, 32, 10, 18), null));
    }

    [Fact]
    public void TheTabALayoutSaysIsKeptAndSurvivesARoundTrip()
    {
        ReportedOccupancy layouts = new(null);
        string line = """{"protocol":1,"type":"layout","grid":"Transfer","columns":10,"rows":19,"items":[],"tab":3}""";
        LayoutReport report = (LayoutReport)GameReport.Parse(line)!;

        layouts.Accept(report);

        Assert.Equal(3, layouts.TabOf("Transfer"));
        Assert.Null(layouts.TabOf("Stash"));
        Assert.Equal(3, ((LayoutReport)GameReport.Parse(report.Write())!).Tab);
    }
}
