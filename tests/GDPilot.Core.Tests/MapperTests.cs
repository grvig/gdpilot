using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class MapperTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(4);

    private readonly Mapper mapper = new(
        new MappingSettings(0.2, 0.2, 0.25, DirectionKeys.Wasd, new CursorSettings(900, 2.0, 2600)),
        new ButtonLayers(
            new Dictionary<GamepadButtons, ButtonBinding> { [GamepadButtons.A] = new KeyBinding(KeyCode.Space) },
            new Dictionary<GamepadButtons, ButtonBinding>(),
            0.5));

    [Fact]
    public void PushingTheLeftStickUpHoldsForward()
    {
        Assert.Equal(new InputIntent[] { new KeyDown(KeyCode.W) }, mapper.Tick(Left(0, 1), Tick));
    }

    [Fact]
    public void DriftInsideTheDeadzoneDoesNothing()
    {
        Assert.Empty(mapper.Tick(Left(0.1, -0.1), Tick));
    }

    [Fact]
    public void CalibratedDriftIsIgnoredEvenOutsideTheDeadzone()
    {
        mapper.LeftCentre = new StickPosition(0.25, 0);

        Assert.Empty(mapper.Tick(Left(0.25, 0), Tick));
    }

    [Fact]
    public void ButtonsAndMovementCombine()
    {
        GamepadSnapshot snapshot = Left(0, 1) with { Buttons = GamepadButtons.A };

        Assert.Equal(new InputIntent[] { new KeyDown(KeyCode.Space), new KeyDown(KeyCode.W) }, mapper.Tick(snapshot, Tick));
    }

    [Fact]
    public void TheRightStickMovesTheCursor()
    {
        GamepadSnapshot snapshot = GamepadSnapshot.Rest with { RightStick = new StickPosition(1, 0) };

        List<InputIntent> intents = mapper.Tick(snapshot, TimeSpan.FromSeconds(0.1));

        Assert.Equal(new InputIntent[] { new PointerMove(90, 0) }, intents);
    }

    [Fact]
    public void LettingGoReleasesWhatWasHeld()
    {
        mapper.Tick(Left(0, 1), Tick);

        Assert.Equal(new InputIntent[] { new KeyUp(KeyCode.W) }, mapper.Tick(GamepadSnapshot.Rest, Tick));
    }

    private static GamepadSnapshot Left(double x, double y)
    {
        return GamepadSnapshot.Rest with { LeftStick = new StickPosition(x, y) };
    }
}
