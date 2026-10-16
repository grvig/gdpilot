using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class RawAxisTests
{
    private const double Tolerance = 1e-9;

    [Fact]
    public void RestingAxesAreCentred()
    {
        Assert.Equal(StickPosition.Centre, RawAxis.Stick(0, 0));
    }

    [Fact]
    public void PushingUpReadsAsPositiveY()
    {
        // Hardware calls up negative.
        StickPosition up = RawAxis.Stick(0, -32768);

        Assert.Equal(1.0, up.Y, Tolerance);
    }

    [Fact]
    public void PushingDownReadsAsNegativeY()
    {
        Assert.Equal(-1.0, RawAxis.Stick(0, 32767).Y, Tolerance);
    }

    [Fact]
    public void FullLeftIsExactlyMinusOne()
    {
        Assert.Equal(-1.0, RawAxis.Stick(short.MinValue, 0).X, Tolerance);
    }

    [Fact]
    public void FullRightIsExactlyOne()
    {
        Assert.Equal(1.0, RawAxis.Stick(short.MaxValue, 0).X, Tolerance);
    }

    [Theory]
    [InlineData((short)0, 0.0)]
    [InlineData((short)32767, 1.0)]
    [InlineData((short)-120, 0.0)]
    public void TriggersRunFromZeroToOne(short raw, double expected)
    {
        Assert.Equal(expected, RawAxis.Trigger(raw), Tolerance);
    }

    [Fact]
    public void AHalfPulledTriggerReadsAboutHalf()
    {
        Assert.InRange(RawAxis.Trigger(16384), 0.49, 0.51);
    }
}
