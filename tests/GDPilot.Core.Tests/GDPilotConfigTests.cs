using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class GDPilotConfigTests
{
    [Fact]
    public void AnEmptyFileTakesEveryDefault()
    {
        GDPilotConfig config = GDPilotConfig.Parse("{}");

        Assert.Equal("Ctrl+Alt+Pause", config.KillSwitch);
        Assert.Equal(250, config.Input.PollHz);
        Assert.Equal(new MappingSettings(0.22, 0.18, 0.25, DirectionKeys.Wasd, new CursorSettings(900, 2.0, 2600)), config.Mapping());
    }

    [Fact]
    public void JumpingBetweenItemsIsOnUnlessTurnedOff()
    {
        Assert.True(GDPilotConfig.Parse("{}").Input.JumpToItems);
        Assert.False(GDPilotConfig.Parse("""{ "input": { "jumpToItems": false } }""").Input.JumpToItems);
    }

    [Fact]
    public void CommentsTrailingCommasAndAnyCaseAreAccepted()
    {
        GDPilotConfig config = GDPilotConfig.Parse("""
            {
              // Lower the poll rate on a slow machine.
              "INPUT": { "pollhz": 125, },
            }
            """);

        Assert.Equal(125, config.Input.PollHz);
    }

    [Fact]
    public void ACombatSectionBecomesButtonLayers()
    {
        GDPilotConfig config = GDPilotConfig.Parse("""{ "bindings": { "Combat": { "A": "key:Space" } } }""");

        GamepadSnapshot pressed = GamepadSnapshot.Rest with { Buttons = GamepadButtons.A };

        Assert.Equal(new[] { KeyCode.Space }, config.ButtonsFor("Combat").Resolve(pressed).Keys);
    }

    [Fact]
    public void AModeWithoutItsOwnSectionUsesCombats()
    {
        GDPilotConfig config = GDPilotConfig.Parse("""{ "bindings": { "Combat": { "A": "key:Space" }, "Inventory": { "A": "mouse:left" } } }""");
        GamepadSnapshot pressed = GamepadSnapshot.Rest with { Buttons = GamepadButtons.A };

        Assert.Equal(new[] { KeyCode.Space }, config.ButtonsFor("Stash").Resolve(pressed).Keys);
        Assert.Equal(new[] { PointerButton.Left }, config.ButtonsFor("Inventory").Resolve(pressed).Buttons);
    }

    [Fact]
    public void PanelKeysDefaultToTheGamesOwn()
    {
        Dictionary<Mode, KeyCode> keys = GDPilotConfig.Parse("{}").PanelKeys();

        Assert.Equal(KeyCode.I, keys[Mode.Inventory]);
        Assert.Equal(KeyCode.K, keys[Mode.Skills]);
        Assert.Equal(KeyCode.Tab, keys[Mode.WorldMap]);
        Assert.False(keys.ContainsKey(Mode.Stash));

        // The character sheet is the inventory window in this game, so it has no key of its own.
        Assert.False(keys.ContainsKey(Mode.Character));
    }

    [Theory]
    [InlineData("""{ "killSwitch": "Hyper+Pause" }""")]
    [InlineData("""{ "input": { "pollHz": 0 } }""")]
    [InlineData("""{ "input": { "deadzone": { "left": 1.0 } } }""")]
    [InlineData("""{ "input": { "cursor": { "maxSpeed": -1 } } }""")]
    [InlineData("""{ "bindings": { "Combat": { "A": "key:NotAKey" } } }""")]
    [InlineData("""{ "bindings": { "Combbat": { "A": "key:Space" } } }""")]
    [InlineData("""{ "bindings": { "Combat": { "A": "panel:Combat" } } }""")]
    [InlineData("""{ "panels": { "Inventory": "Ctrl+I" } }""")]
    [InlineData("""{ "panels": { "Lobby": "L" } }""")]
    public void InvalidValuesAreRejectedAtLoad(string json)
    {
        Assert.Throws<FormatException>(() => GDPilotConfig.Parse(json));
    }

    [Fact]
    public void TheTrackedExampleFileParses()
    {
        GDPilotConfig config = GDPilotConfig.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "config.example.json")));

        Assert.NotEmpty(config.Bindings["Combat"]);
    }
}
