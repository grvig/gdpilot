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
        Assert.Empty(config.ButtonsFor("Stash").Resolve(pressed).Keys);
    }

    [Theory]
    [InlineData("""{ "killSwitch": "Hyper+Pause" }""")]
    [InlineData("""{ "input": { "pollHz": 0 } }""")]
    [InlineData("""{ "input": { "deadzone": { "left": 1.0 } } }""")]
    [InlineData("""{ "input": { "cursor": { "maxSpeed": -1 } } }""")]
    [InlineData("""{ "bindings": { "Combat": { "A": "key:NotAKey" } } }""")]
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
