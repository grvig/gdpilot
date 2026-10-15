using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class SuspensionTests
{
    private readonly RecordingSink recorder = new();
    private readonly GuardedSink sink;
    private readonly Suspension suspension;

    public SuspensionTests()
    {
        sink = new GuardedSink(recorder);
        suspension = new Suspension(sink);
    }

    [Fact]
    public void LosingFocusReleasesHeldKeysAndStopsInjection()
    {
        sink.Send(new KeyDown(KeyCode.W));

        suspension.Suspend(SuspendReason.FocusLost);

        Assert.False(sink.IsEnabled);
        Assert.Equal(new InputIntent[] { new KeyDown(KeyCode.W), new KeyUp(KeyCode.W) }, recorder.Sent);
    }

    [Fact]
    public void RegainingFocusResumesInjection()
    {
        suspension.Suspend(SuspendReason.FocusLost);

        suspension.Resume(SuspendReason.FocusLost);

        Assert.True(sink.IsEnabled);
        Assert.Equal(SuspendReason.None, suspension.Reasons);
    }

    [Fact]
    public void RegainingFocusDoesNotUndoTheKillSwitch()
    {
        suspension.Suspend(SuspendReason.FocusLost);
        suspension.Suspend(SuspendReason.KillSwitch);

        suspension.Resume(SuspendReason.FocusLost);

        Assert.False(sink.IsEnabled);
        Assert.Equal(SuspendReason.KillSwitch, suspension.Reasons);
    }

    [Fact]
    public void ClearingTheKillSwitchWhileUnfocusedStaysSuspended()
    {
        suspension.Suspend(SuspendReason.KillSwitch);
        suspension.Suspend(SuspendReason.FocusLost);

        suspension.Resume(SuspendReason.KillSwitch);

        Assert.False(sink.IsEnabled);
        Assert.Equal(SuspendReason.FocusLost, suspension.Reasons);
    }
}
