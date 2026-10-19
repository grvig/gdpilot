using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class ControlLoopTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(4);

    private readonly FakePad pad = new();
    private readonly RecordingSink recorder = new();
    private readonly Suspension suspension;
    private readonly StallDetector stall;
    private readonly ControlLoop loop;
    private TimeSpan now;
    private bool gameInFront = true;

    public ControlLoopTests()
    {
        GuardedSink guarded = new(recorder);
        suspension = new Suspension(guarded);
        stall = new StallDetector(TimeSpan.FromMilliseconds(100), () => now);
        Mapper mapper = new(
            new MappingSettings(0.2, 0.2, 0.25, DirectionKeys.Wasd, new CursorSettings(900, 2.0, 2600)),
            new ButtonLayers(new Dictionary<GamepadButtons, ButtonBinding>(), new Dictionary<GamepadButtons, ButtonBinding>(), 0.5));
        loop = new ControlLoop(pad, mapper, guarded, stall, new FocusWatcher(() => gameInFront, suspension), suspension);
    }

    [Fact]
    public void APushedStickReachesTheSink()
    {
        pad.Snapshot = Forward();

        loop.Tick(Tick);

        Assert.Equal(new InputIntent[] { new KeyDown(KeyCode.W) }, recorder.Sent);
    }

    [Fact]
    public void NothingIsSentWhileTheGameIsBehind()
    {
        gameInFront = false;
        pad.Snapshot = Forward();

        loop.Tick(Tick);

        Assert.Empty(recorder.Sent);
    }

    [Fact]
    public void AStickStillHeldOnReturnIsPressedAgain()
    {
        pad.Snapshot = Forward();
        loop.Tick(Tick);

        gameInFront = false;
        loop.Tick(Tick);
        gameInFront = true;
        loop.Tick(Tick);

        InputIntent[] expected = { new KeyDown(KeyCode.W), new KeyUp(KeyCode.W), new KeyDown(KeyCode.W) };
        Assert.Equal(expected, recorder.Sent);
    }

    [Fact]
    public void EveryTickKeepsTheStallDetectorQuiet()
    {
        for (int tick = 0; tick < 50; tick++)
        {
            now += TimeSpan.FromMilliseconds(50);
            loop.Tick(Tick);
            Assert.False(stall.CheckForStall());
        }
    }

    private static GamepadSnapshot Forward()
    {
        return GamepadSnapshot.Rest with { LeftStick = new StickPosition(0, 1) };
    }

    private sealed class FakePad : IGamepadSource
    {
        public GamepadSnapshot Snapshot { get; set; } = GamepadSnapshot.Rest;

        public bool IsConnected => true;

        public GamepadSnapshot Read()
        {
            return Snapshot;
        }
    }
}
