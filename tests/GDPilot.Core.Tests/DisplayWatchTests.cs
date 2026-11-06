using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class DisplayWatchTests
{
    private static readonly LiveDisplay Full = new(1920, 1080, 0.5);

    [Fact]
    public void TheFirstDisplaySeenIsAChange()
    {
        Assert.True(new DisplayWatch().Changed(Full));
    }

    [Fact]
    public void TheSameDisplayIsNot()
    {
        DisplayWatch watch = new();
        watch.Changed(Full);

        Assert.False(watch.Changed(Full));
    }

    [Theory]
    [InlineData(1280, 720, 0.5, 0, 0)]
    [InlineData(1920, 1080, 1.0, 0, 0)]
    [InlineData(1920, 1080, 0.5, 40, 0)]
    public void AnotherSizeSettingOrPlaceIs(int width, int height, double uiScale, int left, int top)
    {
        DisplayWatch watch = new();
        watch.Changed(Full);

        Assert.True(watch.Changed(new LiveDisplay(width, height, uiScale, left, top)));
    }

    [Fact]
    public void AGameThatHasGoneChangesNothingAndComingBackTheSameDoesNotEither()
    {
        DisplayWatch watch = new();
        watch.Changed(Full);

        Assert.False(watch.Changed(null));
        Assert.False(watch.Changed(Full));
    }
}
