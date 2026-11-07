using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class GameKeysTests
{
    // The keybindings of a game set up as GDPilot wants, read on 2026-11-08.
    private static readonly string SetUp = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "keybindings.txt"));

    private static readonly GDPilotConfig Shipped = GDPilotConfig.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "config.example.json")));

    [Fact]
    public void AGameSetUpAsGDPilotWantsHasNothingToChange()
    {
        Assert.Empty(GameKeys.Mismatches(SetUp, GameKeys.Wanted(Shipped)));
    }

    [Fact]
    public void TheGamesOwnDefaultOfSkillsOnSIsPointedOut()
    {
        string defaults = SetUp.Replace("6: 37 0", "6: 31 0");

        Assert.Contains("Skill Window on K", GameKeys.Mismatches(defaults, GameKeys.Wanted(Shipped)));
    }

    [Fact]
    public void AMovedRiftKeyIsPointedOut()
    {
        string moved = SetUp.Replace("33: 38 153", "33: 19 0");

        Assert.Equal(new[] { "Personal Riftgate on L" }, GameKeys.Mismatches(moved, GameKeys.Wanted(Shipped)));
    }

    [Fact]
    public void AKeyOnTheSecondaryCountsToo()
    {
        string secondary = SetUp.Replace("34: 17 159", "34: 200 17");

        Assert.Empty(GameKeys.Mismatches(secondary, GameKeys.Wanted(Shipped)));
    }

    [Fact]
    public void WhatGDPilotPressesIncludesMovementPanelsTheQuickbarAndTheRift()
    {
        IReadOnlyList<(string Function, int Line, KeyCode Key)> wanted = GameKeys.Wanted(Shipped);

        Assert.Contains(("Forward", 34, KeyCode.W), wanted);
        Assert.Contains(("Skill Window", 6, KeyCode.K), wanted);
        Assert.Contains(("Quickbar Slot 10", 18, KeyCode.D0), wanted);
        Assert.Contains(("Personal Riftgate", 33, KeyCode.L), wanted);
    }

    [Fact]
    public void AnUnreadableFileAsksNothingOfAnyone()
    {
        Assert.Empty(GameKeys.Mismatches("not a keybindings file", GameKeys.Wanted(Shipped)));
    }

    [Theory]
    [InlineData(KeyCode.W, 17)]
    [InlineData(KeyCode.L, 38)]
    [InlineData(KeyCode.M, 50)]
    [InlineData(KeyCode.D1, 2)]
    [InlineData(KeyCode.D0, 11)]
    [InlineData(KeyCode.Tab, 15)]
    [InlineData(KeyCode.Space, 57)]
    public void KeysAreFiledUnderTheirScanCodes(KeyCode key, int code)
    {
        Assert.Equal(code, GameKeys.ScanCode(key));
    }
}
