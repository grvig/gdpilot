using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class FirstRunTests
{
    [Fact]
    public void WithEverythingInPlaceEachCheckIsReady()
    {
        IReadOnlyList<(bool Ready, string Line)> checks = FirstRun.Checks(new FirstRun.Facts(true, true, true, "1920x1080@0.5", true));

        Assert.Equal(4, checks.Count);
        Assert.All(checks, check => Assert.True(check.Ready));
        Assert.Contains(checks, check => check.Line.Contains("1920x1080@0.5"));
    }

    [Fact]
    public void AMissingPadSaysWhatToDoAndDoesNotHideTheRest()
    {
        IReadOnlyList<(bool Ready, string Line)> checks = FirstRun.Checks(new FirstRun.Facts(false, true, true, "1920x1080@0.5", true));

        Assert.False(checks[0].Ready);
        Assert.Contains("Connect one", checks[0].Line);
        Assert.Equal(4, checks.Count);
    }

    [Fact]
    public void AGameNotRunningLeavesNothingAboutItToCheck()
    {
        IReadOnlyList<(bool Ready, string Line)> checks = FirstRun.Checks(new FirstRun.Facts(true, false, false, null, false));

        Assert.Equal(2, checks.Count);
        Assert.False(checks[1].Ready);
    }

    [Fact]
    public void AGameMissingAFunctionKeepsTheRestOfGDPilotWorking()
    {
        IReadOnlyList<(bool Ready, string Line)> checks = FirstRun.Checks(new FirstRun.Facts(true, true, true, "1280x720@0.5", false));

        Assert.False(checks[^1].Ready);
        Assert.Contains("everything else works", checks[^1].Line);
    }

    [Fact]
    public void TheGamesOwnControllerSupportBeingOnIsPointedOut()
    {
        IReadOnlyList<(bool Ready, string Line)> checks = FirstRun.Checks(new FirstRun.Facts(true, true, true, "1920x1080@0.5", true, "gamepadSupport            = true\nmovementType              = 1\n"));

        Assert.Contains(checks, check => !check.Ready && check.Line.Contains("Turn off Gamepad"));
        Assert.DoesNotContain(checks, check => check.Line.Contains("movement type"));
    }

    [Fact]
    public void AnotherMovementTypeIsPointedOut()
    {
        IReadOnlyList<(bool Ready, string Line)> checks = FirstRun.Checks(new FirstRun.Facts(true, true, true, "1920x1080@0.5", true, "gamepadSupport = false\nmovementType = 0\n"));

        Assert.Contains(checks, check => !check.Ready && check.Line.Contains("movement type"));
        Assert.DoesNotContain(checks, check => check.Line.Contains("Turn off Gamepad"));
    }

    [Fact]
    public void SettingsAsGDPilotWantsThemAddNothing()
    {
        IReadOnlyList<(bool Ready, string Line)> checks = FirstRun.Checks(new FirstRun.Facts(true, true, true, "1920x1080@0.5", true, "gamepadSupport = false\nmovementType = 1\n"));

        Assert.Equal(4, checks.Count);
    }
}
