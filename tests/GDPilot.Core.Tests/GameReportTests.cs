using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class GameReportTests
{
    [Fact]
    public void ALayoutSurvivesTheTripThroughOneLine()
    {
        LayoutReport sent = new("Inventory.Bag2", 8, 8, new[] { new ItemRect(11, 0, 0, 1, 3), new ItemRect(12, 1, 0, 2, 3) });

        string line = sent.Write();
        LayoutReport received = Assert.IsType<LayoutReport>(GameReport.Parse(line));

        Assert.DoesNotContain('\n', line);
        Assert.Equal("Inventory.Bag2", received.Grid);
        Assert.Equal(sent.Items, received.Items);
    }

    [Fact]
    public void OpenPanelsAreReadByName()
    {
        PanelsReport received = Assert.IsType<PanelsReport>(GameReport.Parse("""{"protocol":1,"type":"panels","open":["Inventory","Stash"]}"""));

        Assert.Equal(new[] { "Inventory", "Stash" }, received.Open);
    }

    [Fact]
    public void HealthIsAPercentKeptBetweenNoneAndFull()
    {
        Assert.Equal(new HealthReport(42), GameReport.Parse(new HealthReport(42).Write()));
        Assert.Equal(new HealthReport(100), GameReport.Parse("""{"protocol":1,"type":"health","percent":140}"""));
    }

    [Fact]
    public void TargetsKeepTheirPlaceOnScreenAndTheirLife()
    {
        TargetsReport sent = new(new[] { new SeenTarget(34539, 0.5125, 0.4, 7.5, 80, true), new SeenTarget(34735, 1.16, -0.04, 24.5, 100, false) });

        TargetsReport received = Assert.IsType<TargetsReport>(GameReport.Parse(sent.Write()));

        Assert.Equal(sent.Targets, received.Targets);
    }

    [Fact]
    public void TheCharactersFeetAndTheGroundRoundThemTravelWithTheTargets()
    {
        TargetsReport sent = new(Array.Empty<SeenTarget>())
        {
            Feet = new ScreenSpot(0.5, 0.62),
            Ring = new ScreenSpot?[] { new ScreenSpot(0.5, 0.4), null, new ScreenSpot(0.7, 0.6) },
        };

        TargetsReport received = Assert.IsType<TargetsReport>(GameReport.Parse(sent.Write()));

        Assert.Equal(sent, received);
        Assert.Null(received.Ring[1]);
    }

    [Fact]
    public void AReportWithoutTheGroundStillReads()
    {
        TargetsReport received = Assert.IsType<TargetsReport>(GameReport.Parse("""{"protocol":1,"type":"targets","list":[]}"""));

        Assert.Null(received.Feet);
        Assert.Empty(received.Ring);
    }

    [Fact]
    public void ATargetWithoutAnIdIsRefused()
    {
        Assert.Throws<FormatException>(() => GameReport.Parse("""{"protocol":1,"type":"targets","list":[{"x":0.5,"y":0.5}]}"""));
    }

    [Fact]
    public void AKindOfReportThisVersionDoesNotKnowIsSkipped()
    {
        Assert.Null(GameReport.Parse("""{"protocol":1,"type":"weather","rain":0.5}"""));
    }

    [Fact]
    public void AMismatchedPairSaysToUpdateBoth()
    {
        FormatException problem = Assert.Throws<FormatException>(() => GameReport.Parse("""{"protocol":2,"type":"panels","open":[]}"""));

        Assert.Contains("Update both", problem.Message);
    }

    [Theory]
    [InlineData("""not json""")]
    [InlineData("""{"protocol":1,"type":"layout","columns":8,"rows":8,"items":[]}""")]
    [InlineData("""{"protocol":1,"type":"layout","grid":"Stash","columns":0,"rows":8,"items":[]}""")]
    [InlineData("""{"protocol":1,"type":"layout","grid":"Stash","columns":8,"rows":8,"items":[{"id":0,"column":0,"row":0,"width":1,"height":1}]}""")]
    [InlineData("""{"protocol":1,"type":"layout","grid":"Stash","columns":8,"rows":8,"items":[{"id":3,"column":0,"row":0,"width":1}]}""")]
    public void AMalformedReportIsRefusedWithAReason(string line)
    {
        Assert.Throws<FormatException>(() => GameReport.Parse(line));
    }
}
