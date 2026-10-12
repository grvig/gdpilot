using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class GuardedSinkTests
{
    private readonly RecordingSink recorder = new();

    [Fact]
    public void ReleaseAllLetsGoOfEverythingStillHeld()
    {
        GuardedSink sink = new(recorder);
        sink.Send(new KeyDown(KeyCode.W));
        sink.Send(new KeyDown(KeyCode.A));
        sink.Send(new KeyUp(KeyCode.A));
        sink.Send(new PointerDown(PointerButton.Left));
        recorder.Sent.Clear();

        sink.ReleaseAll();

        Assert.Equal(new InputIntent[] { new KeyUp(KeyCode.W), new PointerUp(PointerButton.Left) }, recorder.Sent);
    }

    [Fact]
    public void NothingIsReleasedTwice()
    {
        GuardedSink sink = new(recorder);
        sink.Send(new KeyDown(KeyCode.W));
        sink.ReleaseAll();
        recorder.Sent.Clear();

        sink.ReleaseAll();

        Assert.Empty(recorder.Sent);
    }

    [Fact]
    public void OneFailedReleaseDoesNotStrandTheOthers()
    {
        FailingOnceSink failing = new(recorder);
        GuardedSink sink = new(failing);
        sink.Send(new KeyDown(KeyCode.W));
        sink.Send(new KeyDown(KeyCode.Q));
        recorder.Sent.Clear();
        failing.FailNext = true;

        Assert.Throws<InvalidOperationException>(() => sink.ReleaseAll());
        Assert.Single(recorder.Sent);
    }

    private sealed class FailingOnceSink : IInputSink
    {
        private readonly IInputSink inner;

        public FailingOnceSink(IInputSink inner)
        {
            this.inner = inner;
        }

        public bool FailNext { get; set; }

        public void Send(InputIntent intent)
        {
            if (FailNext)
            {
                FailNext = false;
                throw new InvalidOperationException("Simulated injection failure.");
            }

            inner.Send(intent);
        }
    }
}
