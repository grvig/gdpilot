using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class StickDirectionsTests
{
    private const double Threshold = 0.3;

    [Theory]
    [InlineData(0.0, 1.0, "W")]
    [InlineData(0.0, -1.0, "S")]
    [InlineData(-1.0, 0.0, "A")]
    [InlineData(1.0, 0.0, "D")]
    [InlineData(0.7, 0.7, "D,W")]
    [InlineData(-0.7, 0.7, "A,W")]
    [InlineData(-0.7, -0.7, "A,S")]
    [InlineData(0.7, -0.7, "D,S")]
    public void EachOfTheEightDirectionsHoldsItsKeys(double x, double y, string expected)
    {
        Assert.Equal(expected, Held(x, y));
    }

    [Fact]
    public void AShallowAngleStaysCardinal()
    {
        // 15 degrees above right is closer to right than to up-right.
        Assert.Equal("D", Held(Math.Cos(Math.PI / 12), Math.Sin(Math.PI / 12)));
    }

    [Fact]
    public void ASteeperAngleBecomesDiagonal()
    {
        // 30 degrees is past the 22.5 degree boundary into up-right.
        Assert.Equal("D,W", Held(Math.Cos(Math.PI / 6), Math.Sin(Math.PI / 6)));
    }

    [Fact]
    public void BelowTheThresholdNothingIsHeld()
    {
        Assert.Equal(string.Empty, Held(0.0, 0.29));
    }

    [Fact]
    public void OtherKeysCanBeUsedInsteadOfWasd()
    {
        DirectionKeys arrows = new(KeyCode.Up, KeyCode.Down, KeyCode.Left, KeyCode.Right);

        IReadOnlySet<KeyCode> held = StickDirections.KeysFor(new StickPosition(0, 1), Threshold, arrows);

        Assert.Equal(new[] { KeyCode.Up }, held);
    }

    private static string Held(double x, double y)
    {
        IReadOnlySet<KeyCode> keys = StickDirections.KeysFor(new StickPosition(x, y), Threshold, DirectionKeys.Wasd);
        return string.Join(",", keys.Select(key => key.ToString()).Order());
    }
}
