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

    /// <summary>
    /// The tour that was played live into the game on 2026-10-24 through --replay,
    /// with the shipped configuration: stash cells, the first bag, both tabs, every
    /// grid in turn, and a Ctrl+click that opened the game's split dialog.
    /// </summary>
    [Fact]
    public void TheStashTourSendsTheCellsTabsAndSplitClickThatWorkedInTheGame()
    {
        GDPilotConfig config = GDPilotConfig.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "config.example.json")));
        GridReadiness readiness = GridReadiness.Check(config, new LiveDisplay(1920, 1080, 0.5));
        GamepadTrace trace = Load("stash-tour.json");
        TimeSpan now = TimeSpan.Zero;
        ReplayGamepadSource source = new(trace, () => now);
        ModeController modes = new(config.PanelKeys(), mode => config.ButtonsFor(mode.ToString()), new GridNavigator(readiness));
        Mapper mapper = new(config.Mapping(), config.ButtonsFor(nameof(Mode.Combat)));
        List<InputIntent> sent = new();

        while (now <= TimeSpan.FromMilliseconds(trace.Entries[^1].At))
        {
            GamepadSnapshot snapshot = source.Read();
            sent.AddRange(modes.Tick(snapshot, Tick));
            mapper.Buttons = modes.CurrentLayers;
            sent.AddRange(mapper.Tick(snapshot, Tick));
            now += Tick;
        }

        Assert.Equal(
            new InputIntent[]
            {
                new PointerMoveTo(529, 293),
                new PointerMoveTo(561, 293),
                new PointerMoveTo(561, 325),
                new PointerMoveTo(870, 645),
                new PointerMoveTo(473, 501),
                new PointerDown(PointerButton.Left),
                new PointerUp(PointerButton.Left),
                new PointerMoveTo(473, 463),
                new PointerDown(PointerButton.Left),
                new PointerUp(PointerButton.Left),
                new PointerMoveTo(1263, 645),
                new PointerMoveTo(529, 293),
                new PointerMoveTo(870, 645),
                new KeyDown(KeyCode.LeftControl),
                new PointerDown(PointerButton.Left),
                new PointerUp(PointerButton.Left),
                new KeyUp(KeyCode.LeftControl),
            },
            sent);
    }

    /// <summary>
    /// The second bag as it was read off the screen on 2026-10-25, when this trace
    /// was played live. The cursor took exactly this path in the game, jumping
    /// the empty cells at (0, 3) and (1, 5).
    /// </summary>
    [Fact]
    public void TheBagTraceJumpsOverEmptyCellsAsItDidInTheGame()
    {
        GDPilotConfig config = GDPilotConfig.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "config.example.json")));
        GridReadiness readiness = GridReadiness.Check(config, new LiveDisplay(1920, 1080, 0.5));
        FixedOccupancy screen = new(
            "#####...",
            "#####...",
            "#####...",
            ".##.....",
            "###.....",
            "#.##....",
            "####....",
            "####....");
        GamepadTrace trace = Load("bag-jumps.json");
        TimeSpan now = TimeSpan.Zero;
        ReplayGamepadSource source = new(trace, () => now);
        ModeController modes = new(config.PanelKeys(), mode => config.ButtonsFor(mode.ToString()), new GridNavigator(readiness, screen));
        List<InputIntent> sent = new();

        while (now <= TimeSpan.FromMilliseconds(trace.Entries[^1].At))
        {
            sent.AddRange(modes.Tick(source.Read(), Tick));
            now += Tick;
        }

        Assert.Equal(
            new InputIntent[]
            {
                new PointerMoveTo(1044, 645),
                new PointerMoveTo(1044, 677),
                new PointerMoveTo(1044, 709),
                new PointerMoveTo(1044, 773),
                new PointerMoveTo(1044, 805),
                new PointerMoveTo(1108, 805),
                new PointerMoveTo(1140, 805),
                new PointerMoveTo(1172, 709),
            },
            sent.OfType<PointerMoveTo>());
    }

    /// <summary>Every grid reads as the same drawn map, # for an item and . for empty.</summary>
    private sealed class FixedOccupancy : IOccupancySource
    {
        private readonly string[] rows;

        public FixedOccupancy(params string[] rows)
        {
            this.rows = rows;
        }

        public OccupancyMap? Read(GridSpec grid, OccupancyMap? previous)
        {
            CellState[,] states = new CellState[grid.Columns, grid.Rows];

            for (int column = 0; column < grid.Columns; column++)
            {
                for (int row = 0; row < grid.Rows; row++)
                {
                    states[column, row] = CellState.Empty;

                    if (row < rows.Length && column < rows[row].Length && rows[row][column] == '#')
                    {
                        states[column, row] = CellState.Occupied;
                    }
                }
            }

            return new OccupancyMap(states);
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
