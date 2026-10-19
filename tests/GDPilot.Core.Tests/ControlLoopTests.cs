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
        ButtonLayers combat = BindingTable.Parse(new Dictionary<string, string> { ["A"] = "key:Space", ["DPadUp"] = "panel:Inventory" }, 0.5);
        ButtonLayers inventory = BindingTable.Parse(new Dictionary<string, string> { ["A"] = "mouse:left", ["B"] = "back" }, 0.5);
        ModeController modes = new(new Dictionary<Mode, KeyCode> { [Mode.Inventory] = KeyCode.I }, mode => LayersFor(mode, combat, inventory));
        Mapper mapper = new(new MappingSettings(0.2, 0.2, 0.25, DirectionKeys.Wasd, new CursorSettings(900, 2.0, 2600)), combat);
        loop = new ControlLoop(pad, modes, mapper, guarded, stall, new FocusWatcher(() => gameInFront, suspension), suspension);
    }

    [Fact]
    public void OpeningAPanelSwitchesTheHeldBindingsToIt()
    {
        pad.Snapshot = GamepadSnapshot.Rest with { Buttons = GamepadButtons.DPadUp };
        loop.Tick(Tick);
        pad.Snapshot = GamepadSnapshot.Rest with { Buttons = GamepadButtons.A };
        loop.Tick(Tick);

        Assert.Equal(Mode.Inventory, loop.Mode);
        Assert.Contains(new PointerDown(PointerButton.Left), recorder.Sent);
        Assert.DoesNotContain(new KeyDown(KeyCode.Space), recorder.Sent);
    }

    private static ButtonLayers LayersFor(Mode mode, ButtonLayers combat, ButtonLayers inventory)
    {
        if (mode == Mode.Inventory)
        {
            return inventory;
        }

        return combat;
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
