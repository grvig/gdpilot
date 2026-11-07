using System.Diagnostics;
using GDPilot.Output;

namespace GDPilot.Integration.Tests;

/// <summary>
/// Both are asked many times a second while GDPilot runs, so each must be cheap
/// after the first answer, and must still give the answer a fresh look would.
/// </summary>
public sealed class ForegroundCostTests
{
    [Fact]
    public void AskingWhetherAProcessIsInFrontAThousandTimesTakesNoTime()
    {
        string front = Process.GetCurrentProcess().ProcessName;
        bool first = ForegroundProbe.IsInFront(front);
        Stopwatch clock = Stopwatch.StartNew();

        for (int ask = 0; ask < 1000; ask++)
        {
            Assert.Equal(first, ForegroundProbe.IsInFront(front));
        }

        Assert.True(clock.ElapsedMilliseconds < 50, $"A thousand asks took {clock.ElapsedMilliseconds} ms.");
    }

    [Fact]
    public void LookingForAGameThatIsNotRunningAThousandTimesTakesNoTime()
    {
        GameWindow.Handle("NoSuchGameAtAll");
        Stopwatch clock = Stopwatch.StartNew();

        for (int ask = 0; ask < 1000; ask++)
        {
            Assert.Equal(IntPtr.Zero, GameWindow.Handle("NoSuchGameAtAll"));
        }

        Assert.True(clock.ElapsedMilliseconds < 50, $"A thousand looks took {clock.ElapsedMilliseconds} ms.");
    }

    [Fact]
    public void ARunningProcessesWindowIsFoundAndKept()
    {
        IntPtr first = GameWindow.Handle("explorer");

        Assert.Equal(first, GameWindow.Handle("explorer"));
    }
}
