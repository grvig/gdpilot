using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class ClosingWindowTests
{
    [Fact]
    public void AFailureInAWindowOfItsOwnWaitsToBeRead()
    {
        Assert.True(ClosingWindow.ShouldWait(2, 1, false));
    }

    [Fact]
    public void ACleanEndNeverWaits()
    {
        Assert.False(ClosingWindow.ShouldWait(0, 1, false));
    }

    [Fact]
    public void AFailureAtACommandPromptStaysOnScreenWithoutWaiting()
    {
        Assert.False(ClosingWindow.ShouldWait(2, 2, false));
    }

    [Fact]
    public void AFailureWithNobodyAtTheConsoleNeverWaits()
    {
        Assert.False(ClosingWindow.ShouldWait(2, 1, true));
    }

    [Fact]
    public void NoConsoleAtAllNeverWaits()
    {
        Assert.False(ClosingWindow.ShouldWait(2, 0, false));
    }
}
