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
