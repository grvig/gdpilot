using GDPilot.Core;
using GDPilot.Output;

namespace GDPilot.Integration.Tests;

[Collection(LoopbackCollection.Name)]
public sealed class SendInputSinkLoopbackTests
{
    private readonly LoopbackFixture loopback;

    public SendInputSinkLoopbackTests(LoopbackFixture loopback)
    {
        this.loopback = loopback;
    }

    [Fact]
    public void IntentsBecomeOperatingSystemEventsInScanCodeMode()
    {
        SendInputSink sink = new();
        int before = loopback.LineCount();

        sink.Send(new KeyDown(KeyCode.W));
        sink.Send(new KeyUp(KeyCode.W));
        sink.Send(new PointerDown(PointerButton.Right));
        sink.Send(new PointerUp(PointerButton.Right));

        List<string> recorded = loopback.WaitForNew(before, lines => lines.Count >= 4);
        Assert.Matches("^key down .*scan=11$", recorded[0]);
        Assert.Matches("^key up .*scan=11$", recorded[1]);
        Assert.StartsWith("mouse rightdown", recorded[2]);
        Assert.StartsWith("mouse rightup", recorded[3]);
    }

    [Fact]
    public void KillSwitchReleasesAHeldKeyAtOperatingSystemLevel()
    {
        GuardedSink guarded = new(new SendInputSink());
        int before = loopback.LineCount();

        guarded.Send(new KeyDown(KeyCode.D));
        guarded.Disable();
        guarded.Send(new KeyDown(KeyCode.A));

        // The second press is refused, so exactly the press and its release arrive.
        List<string> recorded = loopback.WaitForNew(before, lines => lines.Count >= 2);
        Thread.Sleep(200);
        recorded = loopback.WaitForNew(before, lines => lines.Count >= 2);

        Assert.Equal(2, recorded.Count);
        Assert.Matches("^key down .*scan=20$", recorded[0]);
        Assert.Matches("^key up .*scan=20$", recorded[1]);
    }
}
