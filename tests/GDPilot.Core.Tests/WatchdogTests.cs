using System.Diagnostics;
using GDPilot.Core;

namespace GDPilot.Core.Tests;

/// <summary>
/// These run against real time, with margins wide enough that a busy machine
/// does not turn a pass into a failure.
/// </summary>
public sealed class WatchdogTests
{
    private static readonly TimeSpan Threshold = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(20);

    private readonly Stopwatch clock = Stopwatch.StartNew();

    [Fact]
    public void AStalledLoopTriggersTheAction()
    {
        using ManualResetEventSlim fired = new();
        StallDetector detector = new(Threshold, () => clock.Elapsed);
        using Watchdog watchdog = new(detector, Poll, () => fired.Set());

        Assert.True(fired.Wait(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void ALoopThatKeepsBeatingIsLeftAlone()
    {
        int fired = 0;
        StallDetector detector = new(Threshold, () => clock.Elapsed);
        using Watchdog watchdog = new(detector, Poll, () => Interlocked.Increment(ref fired));

        for (int beat = 0; beat < 25; beat++)
        {
            detector.Beat();
            Thread.Sleep(20);
        }

        Assert.Equal(0, Volatile.Read(ref fired));
    }

    [Fact]
    public void AFailingActionDoesNotEndTheWatch()
    {
        using ManualResetEventSlim secondStall = new();
        int calls = 0;
        StallDetector detector = new(Threshold, () => clock.Elapsed);
        using Watchdog watchdog = new(detector, Poll, () =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                detector.Beat();
                throw new InvalidOperationException("Simulated release failure.");
            }

            secondStall.Set();
        });

        Assert.True(secondStall.Wait(TimeSpan.FromSeconds(2)));
        Assert.IsType<InvalidOperationException>(watchdog.LastFailure);
    }
}
