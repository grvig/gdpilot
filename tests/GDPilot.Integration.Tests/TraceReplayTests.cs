using GDPilot.Core;

namespace GDPilot.Integration.Tests;

/// <summary>
/// Scripted controller performances from tests/fixtures/traces, replayed through
/// the real mapper into a recording sink. No pad and no game are involved, and
/// the asserted output is the exact sequence the game would receive.
/// </summary>
public sealed class TraceReplayTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(4);

    [Fact]
    public void WalkingRoundADiagonalPressesAndReleasesInOrder()
    {
        List<InputIntent> sent = Replay("walk-diagonal.json");

        Assert.Equal(
            new InputIntent[]
            {
                new KeyDown(KeyCode.W),
                new KeyDown(KeyCode.D),
                new KeyUp(KeyCode.W),
                new KeyUp(KeyCode.D),
            },
            sent);
    }

    [Fact]
    public void HoldingTheLeftTriggerSwitchesAToTheChordBinding()
    {
        List<InputIntent> sent = Replay("chord-layer.json");

        Assert.Equal(
            new InputIntent[]
            {
                new KeyDown(KeyCode.Space),
                new KeyUp(KeyCode.Space),
                new KeyDown(KeyCode.D1),
                new KeyUp(KeyCode.D1),
            },
            sent);
    }

    [Fact]
    public void OpeningTheInventoryClickingAndBackingOutCrossesModes()
    {
        GamepadTrace trace = Load("inventory-round-trip.json");
        TimeSpan now = TimeSpan.Zero;
        ReplayGamepadSource source = new(trace, () => now);
        ButtonLayers combat = BindingTable.Parse(new Dictionary<string, string> { ["A"] = "key:Space", ["DPadUp"] = "panel:Inventory" }, 0.5);
        ButtonLayers inventory = BindingTable.Parse(new Dictionary<string, string> { ["A"] = "mouse:left", ["B"] = "back" }, 0.5);
        ModeController modes = new(new Dictionary<Mode, KeyCode> { [Mode.Inventory] = KeyCode.I }, LayersFor);
        Mapper mapper = new(Settings, combat);
        List<InputIntent> sent = new();
        List<Mode> visited = new();

        while (now <= TimeSpan.FromMilliseconds(trace.Entries[^1].At))
        {
            GamepadSnapshot snapshot = source.Read();
            sent.AddRange(modes.Tick(snapshot, Tick));
            mapper.Buttons = modes.CurrentLayers;
            sent.AddRange(mapper.Tick(snapshot, Tick));
            visited.Add(modes.Current);
            now += Tick;
        }

        Assert.Equal(
            new InputIntent[]
            {
                new KeyDown(KeyCode.I),
                new KeyUp(KeyCode.I),
                new PointerDown(PointerButton.Left),
                new PointerUp(PointerButton.Left),
                new KeyDown(KeyCode.Escape),
                new KeyUp(KeyCode.Escape),
            },
            sent);
        Assert.Contains(Mode.Inventory, visited);
        Assert.Equal(Mode.Combat, visited[^1]);

        ButtonLayers LayersFor(Mode mode)
        {
            if (mode == Mode.Inventory)
            {
                return inventory;
            }

            return combat;
        }
    }

    private static readonly MappingSettings Settings = new(0.2, 0.2, 0.25, DirectionKeys.Wasd, new CursorSettings(900, 2.0, 2600));

    private static GamepadTrace Load(string fixture)
    {
        return GamepadTrace.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "traces", fixture)));
    }

    private static List<InputIntent> Replay(string fixture)
    {
        GamepadTrace trace = GamepadTrace.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "traces", fixture)));
        TimeSpan now = TimeSpan.Zero;
        ReplayGamepadSource source = new(trace, () => now);
        RecordingSink sink = new();
        Mapper mapper = new(
            new MappingSettings(0.2, 0.2, 0.25, DirectionKeys.Wasd, new CursorSettings(900, 2.0, 2600)),
            new ButtonLayers(
                new Dictionary<GamepadButtons, ButtonBinding> { [GamepadButtons.A] = new KeyBinding(KeyCode.Space) },
                new Dictionary<GamepadButtons, ButtonBinding> { [GamepadButtons.A] = new KeyBinding(KeyCode.D1) },
                0.5));

        while (!source.IsFinished)
        {
            foreach (InputIntent intent in mapper.Tick(source.Read(), Tick))
            {
                sink.Send(intent);
            }

            now += Tick;
        }

        foreach (InputIntent intent in mapper.Tick(source.Read(), Tick))
        {
            sink.Send(intent);
        }

        return sink.Sent;
    }
}
