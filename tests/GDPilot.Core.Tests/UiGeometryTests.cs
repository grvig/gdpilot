using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class UiGeometryTests
{
    private static UiGeometry Between(int width, int height, double uiScale = 0.5, int left = 0, int top = 0)
    {
        return UiGeometry.Between(1920, 1080, 0.5, new LiveDisplay(width, height, uiScale, left, top));
    }

    [Fact]
    public void TheReferenceDisplayMovesNothing()
    {
        UiGeometry same = Between(1920, 1080);

        Assert.Equal(652, same.X(652, UiEdge.Middle));
        Assert.Equal(1059, same.Y(1059, UiEdge.End));
        Assert.Equal(190, same.X(190, UiEdge.Start));
    }

    // Each centre as measured in the reference capture and in a capture at 1280 by 720, within a pixel or two.
    [Theory]
    [InlineData(975, 408, UiEdge.Middle, UiEdge.Middle, 652, 254)]
    [InlineData(965, 560, UiEdge.Middle, UiEdge.Middle, 644, 375)]
    [InlineData(960, 1052, UiEdge.Middle, UiEdge.End, 640, 698)]
    [InlineData(190, 245, UiEdge.Start, UiEdge.Middle, 152, 124)]
    public void PlacesLandWhereTheyWereMeasuredAtASmallerDisplay(double x, double y, UiEdge across, UiEdge down, double wantX, double wantY)
    {
        UiGeometry small = Between(1280, 720);

        Assert.InRange(small.X(x, across), wantX - 2, wantX + 2);
        Assert.InRange(small.Y(y, down), wantY - 2, wantY + 2);
    }

    [Fact]
    public void AWindowOffTheCornerOfTheScreenCarriesEverythingWithIt()
    {
        UiGeometry windowed = Between(1280, 720, 0.5, 320, 180);

        Assert.InRange(windowed.X(975, UiEdge.Middle), 970, 974);
        Assert.InRange(windowed.Y(408, UiEdge.Middle), 432, 436);
    }

    [Fact]
    public void TheSettingScalesItToo()
    {
        Assert.Equal(1.415, Between(1920, 1080, 1.0).Scale, 3);
    }
}
