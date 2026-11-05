using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class DropRumbleTests
{
    private static NearbyThing Loot(uint id, int rarity)
    {
        return new NearbyThing(id, ThingKind.Loot, 0.5, 0.5, 2) { Rarity = rarity };
    }

    private static readonly NearbyThing[] Nothing = Array.Empty<NearbyThing>();

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(9)]
    public void CommonRareAndOddThingsAreNotNews(int rarity)
    {
        DropRumble rumble = new();
        rumble.Update(Nothing);

        Assert.Null(rumble.Update(new[] { Loot(1, rarity) }));
    }

    [Fact]
    public void AnEpicIsFeltOnceAndTheNextOneAgain()
    {
        DropRumble rumble = new();
        rumble.Update(Nothing);

        Assert.NotNull(rumble.Update(new[] { Loot(1, 3) }));
        Assert.Null(rumble.Update(new[] { Loot(1, 3) }));
        Assert.NotNull(rumble.Update(new[] { Loot(1, 3), Loot(2, 3) }));
    }

    [Fact]
    public void AnythingFinerIsFeltMoreStrongly()
    {
        DropRumble rumble = new();
        rumble.Update(Nothing);
        Rumble epic = rumble.Update(new[] { Loot(1, 3) })!.Value;
        Rumble finer = rumble.Update(new[] { Loot(2, 4) })!.Value;

        Assert.True(finer.Heavy > epic.Heavy);
    }

    [Fact]
    public void WhatLayHereAlreadyAsTheFirstReportCameIsNotNews()
    {
        DropRumble rumble = new();
        NearbyThing[] lying = { Loot(1, 4) };

        Assert.Null(rumble.Update(lying));
        Assert.Null(rumble.Update(new[] { Loot(1, 4) }));
    }

    [Fact]
    public void TheSameReportIsOnlyReadOnce()
    {
        DropRumble rumble = new();
        rumble.Update(Nothing);
        NearbyThing[] report = { Loot(1, 3) };

        Assert.NotNull(rumble.Update(report));
        Assert.Null(rumble.Update(report));
    }

    [Fact]
    public void PeopleAreNeverLoot()
    {
        DropRumble rumble = new();
        rumble.Update(Nothing);

        Assert.Null(rumble.Update(new[] { new NearbyThing(1, ThingKind.Talk, 0.5, 0.5, 2) { Rarity = 4 } }));
    }
}
