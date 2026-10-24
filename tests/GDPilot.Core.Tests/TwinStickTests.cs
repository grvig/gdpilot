using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class TwinStickTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(4);

    private static readonly MappingSettings Settings = new(0.2, 0.2, 0.25, DirectionKeys.Wasd, new CursorSettings(900, 2.0, 2600));

    private static readonly ButtonLayers NoButtons = BindingTable.Parse(new Dictionary<string, string>(), 0.5);

    // An overhead ring on a square window, so a reach of 1 is 0.2 of the window from the feet.
    private static readonly TargetsReport Near = new(Array.Empty<SeenTarget>())
    {
        Feet = new ScreenSpot(0.5, 0.5),
        Ring = new ScreenSpot?[] { new ScreenSpot(0.5, 0.3), new ScreenSpot(0.7, 0.5), new ScreenSpot(0.5, 0.7), new ScreenSpot(0.3, 0.5) },
    };

    private static Mapper Aiming()
    {
        return new Mapper(Settings, NoButtons) { TwinStick = true, Near = Near, GameBounds = new ScreenRect(0, 0, 1000, 1000) };
    }

    private static GamepadSnapshot Sticks(double leftX, double leftY, double rightX, double rightY)
    {
        return GamepadSnapshot.Rest with { LeftStick = new StickPosition(leftX, leftY), RightStick = new StickPosition(rightX, rightY) };
    }

    [Fact]
    public void TheRightStickPointsTheCursorAtTheGroundThatWay()
    {
        Mapper mapper = Aiming();

        // Fully over to the right: one and a half of the ring's reach.
        Assert.Contains(new PointerMoveTo(800, 500), mapper.Tick(Sticks(0, 0, 1, 0), Tick));
    }

    [Fact]
    public void AGentlerPushAimsCloser()
    {
        Mapper mapper = Aiming();
        List<InputIntent> sent = mapper.Tick(Sticks(0, 0, 0, 0.6), Tick);

        PointerMoveTo move = Assert.Single(sent.OfType<PointerMoveTo>());
        Assert.Equal(500, move.X);
        Assert.InRange(move.Y, 300, 360);
    }

    [Fact]
    public void LetGoTheAimFollowsTheWalk()
    {
        Mapper mapper = Aiming();
        mapper.Tick(Sticks(0, 0, 1, 0), Tick);

        Assert.Contains(new PointerMoveTo(300, 500), mapper.Tick(Sticks(-1, 0, 0, 0), Tick));
    }

    [Fact]
    public void StandingStillTheAimStaysWhereItWasPointed()
    {
        Mapper mapper = Aiming();
        mapper.Tick(Sticks(0, 0, 0, -1), Tick);

        Assert.Contains(new PointerMoveTo(500, 700), mapper.Tick(GamepadSnapshot.Rest, Tick));
    }

    [Fact]
    public void AnEnemyNearTheAimDrawsTheCursor()
    {
        Mapper mapper = Aiming();
        mapper.SoftAim = new SoftAim();
        mapper.Seen = new[] { new SeenTarget(4, 0.72, 0.52, 6, 100, true) };

        Assert.Contains(new PointerMoveTo(720, 520), mapper.Tick(Sticks(0, 0, 0.5, 0), Tick));
    }

    [Fact]
    public void InAPanelTheCursorIsFreeAgain()
    {
        Mapper mapper = Aiming();
        mapper.InWorld = false;

        List<InputIntent> sent = mapper.Tick(Sticks(0, 0, 1, 0), TimeSpan.FromMilliseconds(20));

        Assert.IsType<PointerMove>(Assert.Single(sent));
    }

    [Fact]
    public void TurnedOffTheRightStickMovesTheCursorAsBefore()
    {
        Mapper mapper = Aiming();
        mapper.TwinStick = false;

        Assert.IsType<PointerMove>(Assert.Single(mapper.Tick(Sticks(0, 0, 1, 0), TimeSpan.FromMilliseconds(20))));
    }
}
