using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class CursorMotionTests
{
    private static readonly CursorSettings Settings = new(900, 2.0, 2600);
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(4);

    private readonly CursorMotion motion = new(Settings);

    [Theory]
    [InlineData(1.0, 900)]
    [InlineData(0.5, 225)]
    [InlineData(0.0, 0)]
    public void SpeedFollowsTheCurve(double magnitude, double expected)
    {
        Assert.Equal(expected, motion.SpeedFor(magnitude), 6);
    }

    [Fact]
    public void SpeedIsClampedToTheMaximum()
    {
        CursorMotion fast = new(new CursorSettings(5000, 1.0, 2600));

        Assert.Equal(2600, fast.SpeedFor(1.0));
    }

    [Fact]
    public void PushingUpMovesTheCursorUpTheScreen()
    {
        PointerMove? move = motion.Step(new StickPosition(0, 1), TimeSpan.FromSeconds(0.1));

        Assert.Equal(new PointerMove(0, -90), move);
    }

    [Fact]
    public void SubPixelMovementAccumulatesAcrossTicks()
    {
        // A tenth deflection is 9 px/s, or 0.036 px per 4 ms tick, so 260 ticks
        // cover 9.36 px. Each tick alone would round to nothing.
        int total = 0;

        for (int tick = 0; tick < 260; tick++)
        {
            total += motion.Step(new StickPosition(0.1, 0), Tick)?.Dx ?? 0;
        }

        Assert.Equal(9, total);
    }

    [Fact]
    public void ReleasingTheStickDropsTheLeftoverFraction()
    {
        // 27 ticks build 0.972 px of carry; one more would cross a whole pixel.
        for (int tick = 0; tick < 27; tick++)
        {
            Assert.Null(motion.Step(new StickPosition(0.1, 0), Tick));
        }

        Assert.Null(motion.Step(StickPosition.Centre, Tick));
        Assert.Null(motion.Step(new StickPosition(0.1, 0), Tick));
    }
}
