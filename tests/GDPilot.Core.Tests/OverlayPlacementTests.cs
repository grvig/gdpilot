using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class OverlayPlacementTests
{
    private static readonly ScreenRect Game = new(100, 50, 1920, 1080);

    [Theory]
    [InlineData(OverlayCorner.TopLeft, 116, 66)]
    [InlineData(OverlayCorner.TopRight, 1704, 66)]
    [InlineData(OverlayCorner.BottomLeft, 116, 1014)]
    [InlineData(OverlayCorner.BottomRight, 1704, 1014)]
    public void TheOverlaySitsInsideTheChosenCornerOfTheGame(OverlayCorner corner, int x, int y)
    {
        Assert.Equal((x, y), OverlayPlacement.Place(Game, 300, 100, corner));
    }

    [Fact]
    public void TheOverlayIsOnAndTopLeftByDefault()
    {
        GDPilotConfig config = GDPilotConfig.Parse("{}");

        Assert.True(config.Overlay.Enabled);
        Assert.Equal(OverlayCorner.TopLeft, config.Overlay.Corner);
    }

    [Fact]
    public void TheCornerIsWrittenByName()
    {
        GDPilotConfig config = GDPilotConfig.Parse("""{ "overlay": { "corner": "BottomRight", "enabled": false } }""");

        Assert.Equal(OverlayCorner.BottomRight, config.Overlay.Corner);
        Assert.False(config.Overlay.Enabled);
    }
}
