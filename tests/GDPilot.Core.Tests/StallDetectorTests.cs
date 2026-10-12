using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class StallDetectorTests
{
    private static readonly TimeSpan Threshold = TimeSpan.FromMilliseconds(250);

    private TimeSpan now;

    [Fact]
    public void ALoopThatKeepsBeatingIsNeverReported()
    {
        StallDetector detector = new(Threshold, () => now);

        for (int tick = 0; tick < 20; tick++)
        {
            Advance(100);
            detector.Beat();
            Assert.False(detector.CheckForStall());
        }
    }

    [Fact]
    public void SilenceUpToTheThresholdIsNotAStall()
    {
        StallDetector detector = new(Threshold, () => now);

        Advance(250);

        Assert.False(detector.CheckForStall());
    }

    [Fact]
    public void AStallIsReportedOnceThenStaysQuiet()
    {
        StallDetector detector = new(Threshold, () => now);

        Advance(251);
        Assert.True(detector.CheckForStall());

        Advance(1000);
        Assert.False(detector.CheckForStall());
    }

    [Fact]
    public void ABeatAfterAStallArmsTheNextOne()
    {
        StallDetector detector = new(Threshold, () => now);
        Advance(300);
        detector.CheckForStall();

        detector.Beat();
        Advance(300);

        Assert.True(detector.CheckForStall());
    }

    private void Advance(int milliseconds)
    {
        now += TimeSpan.FromMilliseconds(milliseconds);
    }
}
