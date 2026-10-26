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
