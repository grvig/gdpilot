using System.Text.Json.Nodes;
using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class ConfigLayersTests
{
    private const string Shipped = """
        {
          "activeProfile": "a",
          "input": { "lookSpeed": 2.5, "rumble": true },
          "bindings": { "Combat": { "A": "key:1", "B": "key:2" } },
          "profiles": { "a": { "uiScale": 0.5 } }
        }
        """;

    private static JsonNode Layered(string own)
    {
        return JsonNode.Parse(ConfigLayers.Over(Shipped, own))!;
    }

    [Fact]
    public void WhatThePersonChangedWinsAndTheRestStands()
    {
        JsonNode settings = Layered("""{ "input": { "lookSpeed": 4 } }""");

        Assert.Equal(4, settings["input"]!["lookSpeed"]!.GetValue<double>());
        Assert.True(settings["input"]!["rumble"]!.GetValue<bool>());
    }

    [Fact]
    public void ABindingOfTheirsLeavesTheOthersInItsTable()
    {
        JsonNode settings = Layered("""{ "bindings": { "Combat": { "A": "key:9" } } }""");

        Assert.Equal("key:9", settings["bindings"]!["Combat"]!["A"]!.GetValue<string>());
        Assert.Equal("key:2", settings["bindings"]!["Combat"]!["B"]!.GetValue<string>());
    }

    [Fact]
    public void SomethingOnlyTheyHaveIsKept()
    {
        JsonNode settings = Layered("""{ "profiles": { "mine": { "uiScale": 1.0 } } }""");

        Assert.Equal(1.0, settings["profiles"]!["mine"]!["uiScale"]!.GetValue<double>());
        Assert.Equal(0.5, settings["profiles"]!["a"]!["uiScale"]!.GetValue<double>());
    }

    [Fact]
    public void AnEmptyFileChangesNothing()
    {
        Assert.Equal(JsonNode.Parse(Shipped)!.ToJsonString(), Layered("{}").ToJsonString());
    }

    [Fact]
    public void CommentsAndTrailingCommasInTheirFileAreForgiven()
    {
        JsonNode settings = Layered("""
            {
              // Slower, for the couch.
              "input": { "lookSpeed": 1, },
            }
            """);

        Assert.Equal(1, settings["input"]!["lookSpeed"]!.GetValue<double>());
    }

    [Fact]
    public void WhatIsNotAnObjectIsCalledInvalid()
    {
        Assert.Throws<FormatException>(() => ConfigLayers.Over(Shipped, "[1]"));
    }

    [Fact]
    public void APersonsCopyLeavesOutTheMeasuredPlaces()
    {
        JsonNode copy = JsonNode.Parse(ConfigLayers.ForAPerson(Shipped))!;

        Assert.Null(copy["profiles"]);
        Assert.Equal(2.5, copy["input"]!["lookSpeed"]!.GetValue<double>());
    }

    [Fact]
    public void AnUpdatesNewBindingReachesAPersonsOldCopy()
    {
        string old = """{ "bindings": { "Combat": { "A": "key:1" } } }""";

        JsonNode settings = JsonNode.Parse(ConfigLayers.Over(Shipped, old))!;

        Assert.Equal("key:2", settings["bindings"]!["Combat"]!["B"]!.GetValue<string>());
    }
}
