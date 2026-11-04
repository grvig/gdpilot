using GDPilot.Core;

namespace GDPilot.Core.Tests;

/// <summary>Leaving conversation mode once its box has closed, though the game's talk report lingers.</summary>
public sealed class ConversationEndTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(10);

    private static readonly ButtonLayers Layers = BindingTable.Parse(
        new Dictionary<string, string> { ["DPadDown"] = "grid:down", ["B"] = "back" },
        0.5);

    private static readonly GridReadiness Ready = new(true, "ready", new Dictionary<string, GridSpec>())
    {
        Spots = new Dictionary<string, SpotArea>
        {
            ["Dialog"] = new SpotArea(773, 300, 477, 635, null, Array.Empty<ScreenPoint>(), SpotKind.Replies),
        },
    };

    private sealed class Replies : ISpotSource
    {
        public List<ScreenPoint> Showing { get; set; } = new() { new(833, 886) };

        public List<ScreenPoint>? Read(SpotArea area)
        {
            return new List<ScreenPoint>(Showing);
        }
    }

    private static void Press(ModeController modes, GamepadButtons button)
    {
        modes.Tick(GamepadSnapshot.Rest with { Buttons = button }, Tick);

        for (int index = 0; index < 40; index++)
        {
            modes.Tick(GamepadSnapshot.Rest, Tick);
        }
    }

    [Fact]
    public void APressWhileRepliesShowStaysInTheConversation()
    {
        ModeController modes = new(new Dictionary<Mode, KeyCode>(), _ => Layers, new GridNavigator(Ready, null, new Replies()));
        modes.Follow(new[] { "Dialog" });

        Press(modes, GamepadButtons.DPadDown);

        Assert.Equal(Mode.Dialog, modes.Current);
    }

    [Fact]
    public void APressAfterTheBoxClosedLeavesForGood()
    {
        Replies replies = new();
        ModeController modes = new(new Dictionary<Mode, KeyCode>(), _ => Layers, new GridNavigator(Ready, null, replies));
        modes.Follow(new[] { "Dialog" });
        replies.Showing = new List<ScreenPoint>();

        Press(modes, GamepadButtons.DPadDown);
        modes.Follow(new[] { "Dialog" });

        Assert.Equal(Mode.Combat, modes.Current);
    }
}
