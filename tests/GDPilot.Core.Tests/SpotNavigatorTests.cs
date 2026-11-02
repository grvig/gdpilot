using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class SpotNavigatorTests
{
    // A corner of the skill tree as found on screen: centres a few pixels off their rows.
    private static readonly ScreenPoint[] Tree =
    {
        new(734, 326), new(896, 320), new(1058, 326),
        new(970, 358),
        new(814, 392), new(896, 396),
        new(734, 452), new(974, 458),
    };

    [Fact]
    public void TheFirstSpotIsTheOneNearestTheTopLeft()
    {
        Assert.Equal(new ScreenPoint(734, 326), SpotNavigator.First(Tree));
    }

    [Fact]
    public void NoSpotsMeansNowhereToStart()
    {
        Assert.Null(SpotNavigator.First(Array.Empty<ScreenPoint>()));
    }

    [Fact]
    public void RightStaysInTheRowDespiteCentresBeingAFewPixelsOff()
    {
        Assert.Equal(new ScreenPoint(896, 320), SpotNavigator.Next(Tree, new ScreenPoint(734, 326), GridDirection.Right));
    }

    [Fact]
    public void DownPrefersStraightBelowOverNearerButSideways()
    {
        Assert.Equal(new ScreenPoint(896, 396), SpotNavigator.Next(Tree, new ScreenPoint(896, 320), GridDirection.Down));
    }

    [Fact]
    public void ASpotInTheSameRowIsNeverBelow()
    {
        ScreenPoint[] row = { new(100, 100), new(180, 112) };

        Assert.Null(SpotNavigator.Next(row, new ScreenPoint(100, 100), GridDirection.Down));
    }

    [Fact]
    public void NothingThatWayLeavesTheCursorWhereItIs()
    {
        Assert.Null(SpotNavigator.Next(Tree, new ScreenPoint(734, 326), GridDirection.Left));
    }

    [Fact]
    public void ADiagonalNeighbourIsReachedWhenNothingIsStraightAhead()
    {
        ScreenPoint[] spots = { new(734, 326), new(896, 320), new(814, 392) };

        Assert.Equal(new ScreenPoint(814, 392), SpotNavigator.Next(spots, new ScreenPoint(734, 326), GridDirection.Down));
    }
}
