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
    public void AnOrdinaryStartInAWindowOfItsOwnGoesToTheTray()
    {
        Assert.True(ClosingWindow.ShouldGoToTray(false, true, 1, false));
    }

    [Fact]
    public void AFirstRunKeepsItsWindowForTheSetupNotes()
    {
        Assert.False(ClosingWindow.ShouldGoToTray(true, true, 1, false));
    }

    [Fact]
    public void ACommandPromptADryRunOrAReplayKeepTheirWindow()
    {
        Assert.False(ClosingWindow.ShouldGoToTray(false, true, 2, false));
        Assert.False(ClosingWindow.ShouldGoToTray(false, false, 1, false));
        Assert.False(ClosingWindow.ShouldGoToTray(false, true, 1, true));
    }

    [Fact]
    public void NoConsoleAtAllNeverWaits()
    {
        Assert.False(ClosingWindow.ShouldWait(2, 0, false));
    }
}
