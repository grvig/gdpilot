using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class GroundAimTests
{
    private const double Aspect = 1;

    // A flat ring, as a camera straight overhead would see it, so expected points are easy to reason about.
    private static readonly TargetsReport Flat = new(Array.Empty<SeenTarget>())
    {
        Feet = new ScreenSpot(0.5, 0.5),
        Ring = new ScreenSpot?[]
        {
            new ScreenSpot(0.5, 0.3),
            new ScreenSpot(0.7, 0.5),
            new ScreenSpot(0.5, 0.7),
            new ScreenSpot(0.3, 0.5),
        },
    };

    [Fact]
    public void AnAngleOnARingPointLandsOnIt()
    {
        ScreenSpot spot = GroundAim.Spot(Flat, 0, 1, Aspect);

        Assert.Equal(0.7, spot.X, 6);
        Assert.Equal(0.5, spot.Y, 6);
    }

    [Fact]
    public void AnAngleBetweenTwoPointsBlendsThem()
    {
        ScreenSpot spot = GroundAim.Spot(Flat, -Math.PI / 4, 1, Aspect);

        Assert.Equal(0.6, spot.X, 6);
        Assert.Equal(0.4, spot.Y, 6);
    }

    [Fact]
    public void ReachDrawsThePointInOrPushesItOut()
    {
        Assert.Equal(0.6, GroundAim.Spot(Flat, 0, 0.5, Aspect).X, 6);
        Assert.Equal(0.8, GroundAim.Spot(Flat, 0, 1.5, Aspect).X, 6);
    }

    [Fact]
    public void AMissingPointIsBridgedByItsNeighbours()
    {
        TargetsReport gap = Flat with { Ring = new ScreenSpot?[] { new ScreenSpot(0.5, 0.3), new ScreenSpot(0.7, 0.5), null, new ScreenSpot(0.3, 0.5) } };

        ScreenSpot spot = GroundAim.Spot(gap, Math.PI / 2, 1, Aspect);

        Assert.Equal(0.5, spot.X, 6);
        Assert.Equal(0.5, spot.Y, 6);
    }

    [Fact]
    public void WithoutTheGameItAimsOutFromTheMiddle()
    {
        ScreenSpot spot = GroundAim.Spot(null, -Math.PI / 2, 1, Aspect);

        Assert.Equal(0.5, spot.X, 6);
        Assert.Equal(0.3, spot.Y, 6);
    }
}
