using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class InteractMappingTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(4);

    private static readonly MappingSettings Settings = new(0.2, 0.2, 0.25, DirectionKeys.Wasd, new CursorSettings(900, 2.0, 2600));

    private static readonly ButtonLayers Combat = BindingTable.Parse(new Dictionary<string, string> { ["A"] = "interact:Space" }, 0.5);

    private static readonly GamepadSnapshot Press = GamepadSnapshot.Rest with { Buttons = GamepadButtons.A };

    private static Mapper Near(params NearbyThing[] things)
    {
        return new Mapper(Settings, Combat) { Things = things, GameBounds = new ScreenRect(0, 0, 1920, 1080), Looking = true, AimPoint = (960, 410) };
    }

    [Fact]
    public void APressClicksTheThingOffered()
    {
        Mapper mapper = Near(new NearbyThing(38371, ThingKind.Talk, 0.1621, 0.4733, 5));

        List<InputIntent> sent = mapper.Tick(Press, Tick);

        int moved = sent.IndexOf(new PointerMoveTo(311, 511));
        Assert.True(moved >= 0 && sent.IndexOf(new PointerDown(PointerButton.Left)) > moved);
        Assert.DoesNotContain(new KeyDown(KeyCode.Space), sent);
    }

    [Fact]
    public void TheClickIsLetGoAfterATapAndTheCursorGoesBack()
    {
        Mapper mapper = Near(new NearbyThing(1, ThingKind.Loot, 0.5, 0.6, 2));
        mapper.Tick(Press, Tick);
        List<InputIntent> sent = new();

        for (int tick = 0; tick < 12; tick++)
        {
            sent.AddRange(mapper.Tick(GamepadSnapshot.Rest, Tick));
        }

        Assert.Contains(new PointerUp(PointerButton.Left), sent);
        Assert.Contains(new PointerMoveTo(960, 410), sent);
    }

    [Fact]
    public void WithNothingNearTheButtonHoldsItsOwnKey()
    {
        Mapper mapper = Near();

        Assert.Contains(new KeyDown(KeyCode.Space), mapper.Tick(Press, Tick));
        Assert.DoesNotContain(new KeyUp(KeyCode.Space), mapper.Tick(Press, Tick));
        Assert.Contains(new KeyUp(KeyCode.Space), mapper.Tick(GamepadSnapshot.Rest, Tick));
    }

    [Fact]
    public void TheOfferIsKeptForThePrompt()
    {
        Mapper mapper = Near(new NearbyThing(1, ThingKind.Use, 0.5, 0.6, 4), new NearbyThing(2, ThingKind.Talk, 0.2, 0.6, 8));

        mapper.Tick(GamepadSnapshot.Rest, Tick);

        Assert.Equal(1u, mapper.Offered!.Id);
    }

    [Fact]
    public void InAPanelNothingIsOffered()
    {
        Mapper mapper = Near(new NearbyThing(1, ThingKind.Use, 0.5, 0.6, 4));
        mapper.InWorld = false;

        mapper.Tick(GamepadSnapshot.Rest, Tick);

        Assert.Null(mapper.Offered);
    }
}
