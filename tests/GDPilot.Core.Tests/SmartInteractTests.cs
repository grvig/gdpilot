using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class SmartInteractTests
{
    [Fact]
    public void TheNearestThingWithinReachIsOffered()
    {
        NearbyThing[] things = { new(1, ThingKind.Talk, 0.2, 0.5, 5), new(2, ThingKind.Use, 0.6, 0.5, 3), new(3, ThingKind.Talk, 0.5, 0.4, 9) };

        Assert.Equal(2u, SmartInteract.Offer(things)!.Id);
    }

    [Fact]
    public void LootWinsANearTie()
    {
        NearbyThing[] things = { new(1, ThingKind.Use, 0.6, 0.5, 2.5), new(2, ThingKind.Loot, 0.5, 0.6, 3) };

        Assert.Equal(2u, SmartInteract.Offer(things)!.Id);
    }

    [Fact]
    public void NothingWithinReachOffersNothing()
    {
        Assert.Null(SmartInteract.Offer(new[] { new NearbyThing(1, ThingKind.Talk, 0.2, 0.5, 7) }));
    }

    [Fact]
    public void AnInteractBindingNamesItsFallbackAndReadsPlainly()
    {
        ButtonLayers combat = BindingTable.Parse(new Dictionary<string, string> { ["A"] = "interact:Space" }, 0.5);

        Assert.Equal(new InteractBinding(KeyCode.Space), combat.BindingFor(GamepadButtons.A, 0));
        Assert.Contains("A: Interact, else Space", OverlayState.Build(Mode.Combat, SuspendReason.None, combat).Legend);
        Assert.Equal("Pick up", SmartInteract.Verb(ThingKind.Loot));
    }
}
