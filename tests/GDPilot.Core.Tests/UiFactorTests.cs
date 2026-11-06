using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class UiFactorTests
{
    // Measured from captures of the game at each display.
    [Theory]
    [InlineData(1920, 1080, 0.5, 1.0)]
    [InlineData(1600, 900, 0.5, 0.86)]
    [InlineData(1280, 720, 0.5, 0.80)]
    public void TheFactorIsWhatTheGameWasMeasuredDrawing(int width, int height, double uiScale, double measured)
    {
        Assert.Equal(measured, UiFactor.Of(width, height, uiScale), 2);
    }

    [Fact]
    public void AVeryLargeWindowFollowsTheSettingAlone()
    {
        Assert.Equal(1.0, UiFactor.Of(3840, 2160, 0.5), 3);
        Assert.Equal(2.0, UiFactor.Of(3840, 2160, 1.0), 3);
    }

    [Fact]
    public void ASettingBelowAHalfShrinksItWhateverTheWindow()
    {
        Assert.Equal(0.7, UiFactor.Of(1920, 1080, 0), 3);
        Assert.Equal(0.7, UiFactor.Of(1280, 720, 0), 3);
    }

    [Fact]
    public void ALargerSettingGrowsItMoreOnALargerWindow()
    {
        Assert.Equal(1.415, UiFactor.Of(1920, 1080, 1.0), 3);
        Assert.Equal(1.04, UiFactor.Of(1280, 720, 1.0), 3);
    }
}
