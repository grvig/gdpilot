using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class SoftAimTests
{
    private const double Aspect = 16.0 / 9;

    private static SeenTarget Enemy(uint id, double x, double y, double distance = 6, bool onScreen = true)
    {
        return new SeenTarget(id, x, y, distance, 100, onScreen);
    }

    [Fact]
    public void AnEnemyNearTheAimPointDrawsTheCursor()
    {
        SoftAim aim = new();

        Assert.Equal(2u, aim.Pick(new[] { Enemy(1, 0.1, 0.4), Enemy(2, 0.53, 0.42) }, 0.5, 0.38, Aspect)!.Id);
    }

    [Fact]
    public void EnemiesFarFromTheAimPointOrTheCharacterAreLeftAlone()
    {
        SoftAim aim = new();

        Assert.Null(aim.Pick(new[] { Enemy(1, 0.8, 0.4), Enemy(2, 0.51, 0.39, distance: 20), Enemy(3, 0.5, 0.38, onScreen: false) }, 0.5, 0.38, Aspect));
    }

    [Fact]
    public void TheChosenEnemyIsKeptAsAnotherDrawsLevel()
    {
        SoftAim aim = new();
        aim.Pick(new[] { Enemy(1, 0.52, 0.38), Enemy(2, 0.56, 0.38) }, 0.5, 0.38, Aspect);

        SeenTarget? kept = aim.Pick(new[] { Enemy(1, 0.54, 0.38), Enemy(2, 0.53, 0.38) }, 0.5, 0.38, Aspect);

        Assert.Equal(1u, kept!.Id);
    }

    [Fact]
    public void TheChosenEnemyIsKeptALittleBeyondTheCircleThatTookIt()
    {
        SoftAim aim = new();
        aim.Pick(new[] { Enemy(1, 0.52, 0.38) }, 0.5, 0.38, Aspect);

        Assert.NotNull(aim.Pick(new[] { Enemy(1, 0.6, 0.38) }, 0.5, 0.38, Aspect));
        Assert.Null(aim.Pick(new[] { Enemy(1, 0.7, 0.38) }, 0.5, 0.38, Aspect));
    }
}
