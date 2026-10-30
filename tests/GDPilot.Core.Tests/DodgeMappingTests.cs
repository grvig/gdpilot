using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class DodgeMappingTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(4);

    private static readonly MappingSettings Settings = new(0.2, 0.2, 0.25, DirectionKeys.Wasd, new CursorSettings(900, 2.0, 2600), 2.5, 0.6);

    private static readonly ButtonLayers Combat = BindingTable.Parse(new Dictionary<string, string> { ["B"] = "dodge:4", ["RightStick"] = "lock:toggle" }, 0.5);

    private static readonly TargetsReport Near = new(new[] { new SeenTarget(11, 0.5, 0.4, 6, 100, true) })
    {
        Feet = new ScreenSpot(0.5, 0.7),
        Ring = new ScreenSpot?[] { new ScreenSpot(0.5, 0.45), null, new ScreenSpot(0.75, 0.68), null, null, null, new ScreenSpot(0.25, 0.68), null },
    };

    private static Mapper Dodging()
    {
        return new Mapper(Settings, Combat)
        {
            Lock = new LockOn(new LockSettings()),
            Near = Near,
            Seen = Near.Targets,
            GameBounds = new ScreenRect(0, 0, 1920, 1080),
            Looking = true,
            AimPoint = (960, 410),
        };
    }

    private static GamepadSnapshot Dodge(double leftX)
    {
        return GamepadSnapshot.Rest with { Buttons = GamepadButtons.B, LeftStick = new StickPosition(leftX, 0) };
    }

    [Fact]
    public void ADodgePutsTheCursorWhereTheStickPointsThenTapsTheKey()
    {
        Mapper mapper = Dodging();

        List<InputIntent> sent = mapper.Tick(Dodge(1), Tick);

        int moved = sent.IndexOf(new PointerMoveTo(1440, 734));
        int pressed = sent.IndexOf(new KeyDown(KeyCode.D4));
        Assert.True(moved >= 0 && pressed > moved);
    }

    [Fact]
    public void TheKeyIsLetGoAfterATapAndTheCursorGoesBack()
    {
        Mapper mapper = Dodging();
        mapper.Tick(Dodge(1), Tick);

        List<InputIntent> sent = new();

        for (int tick = 0; tick < 12; tick++)
        {
            sent.AddRange(mapper.Tick(GamepadSnapshot.Rest, Tick));
        }

        Assert.Contains(new KeyUp(KeyCode.D4), sent);
        Assert.Contains(new PointerMoveTo(960, 410), sent);
    }

    [Fact]
    public void WhileTheKeyIsDownTheLockDoesNotTakeTheCursorBack()
    {
        Mapper mapper = Dodging();
        mapper.Tick(GamepadSnapshot.Rest with { Buttons = GamepadButtons.RightStick }, Tick);
        mapper.Tick(GamepadSnapshot.Rest, Tick);
        mapper.Tick(Dodge(-1), Tick);

        Assert.DoesNotContain(mapper.Tick(Dodge(-1), Tick), intent => intent is PointerMoveTo);
    }

    [Fact]
    public void HoldingTheButtonDodgesOnce()
    {
        Mapper mapper = Dodging();
        List<InputIntent> sent = new();

        for (int tick = 0; tick < 40; tick++)
        {
            sent.AddRange(mapper.Tick(Dodge(1), Tick));
        }

        Assert.Single(sent, intent => intent == new KeyDown(KeyCode.D4));
    }

    [Fact]
    public void WithoutTheGameWindowTheDodgeStillFires()
    {
        Mapper mapper = Dodging();
        mapper.GameBounds = null;

        List<InputIntent> sent = mapper.Tick(Dodge(1), Tick);

        Assert.Contains(new KeyDown(KeyCode.D4), sent);
        Assert.DoesNotContain(sent, intent => intent is PointerMoveTo);
    }
}
