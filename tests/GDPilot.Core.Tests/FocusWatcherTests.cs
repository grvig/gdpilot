using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class FocusWatcherTests
{
    private readonly RecordingSink recorder = new();
    private readonly GuardedSink sink;
    private readonly Suspension suspension;
    private readonly FocusWatcher watcher;
    private bool gameInFront;

    public FocusWatcherTests()
    {
        sink = new GuardedSink(recorder);
        suspension = new Suspension(sink);
        watcher = new FocusWatcher(() => gameInFront, suspension);
    }

    [Fact]
    public void StartingWithTheGameBehindSuspendsAtOnce()
    {
        watcher.Poll();

        Assert.Equal(SuspendReason.FocusLost, suspension.Reasons);
    }

    [Fact]
    public void AltTabbingAwayReleasesHeldKeysOnce()
    {
        gameInFront = true;
        watcher.Poll();
        sink.Send(new KeyDown(KeyCode.W));

        gameInFront = false;
        watcher.Poll();
        watcher.Poll();

        Assert.Equal(new InputIntent[] { new KeyDown(KeyCode.W), new KeyUp(KeyCode.W) }, recorder.Sent);
    }

    [Fact]
    public void ComingBackResumesInjection()
    {
        watcher.Poll();

        gameInFront = true;
        watcher.Poll();

        Assert.True(sink.IsEnabled);
    }
}
