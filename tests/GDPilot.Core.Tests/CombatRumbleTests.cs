using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class CombatRumbleTests
{
    private static readonly TimeSpan Report = TimeSpan.FromMilliseconds(33);

    private static SeenTarget[] Enemy(int health, double distance = 4, uint id = 7)
    {
        return new[] { new SeenTarget(id, 0.5, 0.4, distance, health, true) };
    }

    [Fact]
    public void ANearbyEnemyLosingLifeTicksLightly()
    {
        CombatRumble rumble = new();
        rumble.Update(Enemy(100), null, Report);

        Rumble? felt = rumble.Update(Enemy(96), null, Report);

        Assert.Equal(0, felt!.Value.Heavy);
        Assert.True(felt.Value.Light > 0);
    }

    [Fact]
    public void ABigPieceOfLifeInOneBlowKnocksHarder()
    {
        CombatRumble rumble = new();
        rumble.Update(Enemy(100), null, Report);

        Assert.True(rumble.Update(Enemy(70), null, Report)!.Value.Heavy > 0);
    }

    [Fact]
    public void ANearlyDeadEnemyVanishingThumps()
    {
        CombatRumble rumble = new();
        rumble.Update(Enemy(12), null, Report);

        Rumble? felt = rumble.Update(Array.Empty<SeenTarget>(), null, Report);

        Assert.True(felt!.Value.Heavy >= 0.5);
    }

    [Fact]
    public void AHealthyEnemyWalkingOutOfReachIsNoKill()
    {
        CombatRumble rumble = new();
        rumble.Update(Enemy(100, distance: 11), null, Report);

        Assert.Null(rumble.Update(Array.Empty<SeenTarget>(), null, Report));
    }

    [Fact]
    public void AFarEnemyIsFeltOnlyWhenLocked()
    {
        CombatRumble rumble = new();
        rumble.Update(Enemy(100, distance: 20), null, Report);
        Assert.Null(rumble.Update(Enemy(95, distance: 20), null, Report));

        Assert.NotNull(rumble.Update(Enemy(90, distance: 20), 7, Report));
    }

    [Fact]
    public void AFlurryTicksNoFasterThanItsGap()
    {
        CombatRumble rumble = new();
        int ticks = 0;

        for (int health = 100; health > 80; health--)
        {
            if (rumble.Update(Enemy(health), null, Report) is not null)
            {
                ticks++;
            }
        }

        Assert.InRange(ticks, 4, 6);
    }

    [Fact]
    public void TheSameReportIsReadOnce()
    {
        CombatRumble rumble = new();
        rumble.Update(Enemy(100), null, Report);
        SeenTarget[] hurt = Enemy(90);

        Assert.NotNull(rumble.Update(hurt, null, Report));
        Assert.Null(rumble.Update(hurt, null, TimeSpan.FromSeconds(1)));
    }
}
