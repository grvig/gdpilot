using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class SteeringTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(4);

    private static readonly MappingSettings Settings = new(0.2, 0.2, 0.25, DirectionKeys.Wasd, new CursorSettings(900, 2.0, 2600));

    private static readonly ButtonLayers NoButtons = BindingTable.Parse(new Dictionary<string, string>(), 0.5);

    private static GamepadSnapshot Left(double x, double y)
    {
        return GamepadSnapshot.Rest with { LeftStick = new StickPosition(x, y) };
    }

    private static IEnumerable<MoveCommand> Moves(IEnumerable<InputIntent> sent)
    {
        return sent.OfType<GameCommandIntent>().Select(intent => intent.Command).OfType<MoveCommand>();
    }

    [Fact]
    public void SteeringSendsTheSticksDirectionAlongsideTheKeysThatWalk()
    {
        Mapper mapper = new(Settings, NoButtons) { Steering = true };

        List<InputIntent> sent = mapper.Tick(Left(0.5, 0.5), Tick);

        Assert.Contains(new KeyDown(KeyCode.W), sent);
        Assert.Contains(new KeyDown(KeyCode.D), sent);
        Assert.Equal(new[] { new MoveCommand(0.707, 0.707) }, Moves(sent));
    }

    [Fact]
    public void AnyAngleIsKeptNotJustEight()
    {
        Mapper mapper = new(Settings, NoButtons) { Steering = true };

        MoveCommand move = Moves(mapper.Tick(Left(0.26, 0.97), Tick)).Single();

        Assert.Equal(Math.Atan2(0.97, 0.26), Math.Atan2(move.Y, move.X), 2);
    }

    [Fact]
    public void AHeldDirectionIsSentAgainSoTheGameKnowsGDPilotIsThere()
    {
        Mapper mapper = new(Settings, NoButtons) { Steering = true };
        List<InputIntent> sent = new();

        for (int tick = 0; tick < 100; tick++)
        {
            sent.AddRange(mapper.Tick(Left(0, 1), Tick));
        }

        Assert.InRange(Moves(sent).Count(), 3, 4);
    }

    [Fact]
    public void LettingGoStopsTheCharacterOnce()
    {
        Mapper mapper = new(Settings, NoButtons) { Steering = true };
        mapper.Tick(Left(0, 1), Tick);

        List<InputIntent> sent = new();

        for (int tick = 0; tick < 100; tick++)
        {
            sent.AddRange(mapper.Tick(GamepadSnapshot.Rest, Tick));
        }

        Assert.Equal(new[] { new MoveCommand(0, 0) }, Moves(sent));
    }

    [Fact]
    public void WithoutTheGameTheKeysStillWalk()
    {
        Mapper mapper = new(Settings, NoButtons);

        List<InputIntent> sent = mapper.Tick(Left(0, 1), Tick);

        Assert.Contains(new KeyDown(KeyCode.W), sent);
        Assert.Empty(Moves(sent));
    }

    [Fact]
    public void LosingTheGameMidStepLeavesTheKeysWalking()
    {
        Mapper mapper = new(Settings, NoButtons) { Steering = true };
        mapper.Tick(Left(0, 1), Tick);
        mapper.Steering = false;

        List<InputIntent> sent = mapper.Tick(Left(0, 1), Tick);

        Assert.DoesNotContain(new KeyUp(KeyCode.W), sent);
        Assert.Equal(new[] { new MoveCommand(0, 0) }, Moves(sent));
    }
}
