using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class InGameAutoLoadTests
{
    private static readonly TimeSpan Start = TimeSpan.FromSeconds(100);

    [Fact]
    public void NoGameMeansNoLoad()
    {
        Assert.False(new InGameAutoLoad().Due(null, false, Start));
    }

    [Fact]
    public void AGameJustStartedIsGivenTimeToSettle()
    {
        InGameAutoLoad load = new();

        Assert.False(load.Due(7, false, Start));
        Assert.False(load.Due(7, false, Start + InGameAutoLoad.Settle - TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void ASettledGameIsLoadedOnceOnly()
    {
        InGameAutoLoad load = new();
        load.Due(7, false, Start);

        Assert.True(load.Due(7, false, Start + InGameAutoLoad.Settle));
        Assert.False(load.Due(7, false, Start + InGameAutoLoad.Settle + TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void APartAlreadyTalkingNeedsNoLoad()
    {
        InGameAutoLoad load = new();
        load.Due(7, true, Start);

        Assert.False(load.Due(7, true, Start + InGameAutoLoad.Settle));
        Assert.True(load.Settled);
    }

    [Fact]
    public void ARestartedGameIsLoadedAgainAfterSettling()
    {
        InGameAutoLoad load = new();
        load.Due(7, false, Start);
        load.Due(7, false, Start + InGameAutoLoad.Settle);

        TimeSpan later = Start + TimeSpan.FromMinutes(5);
        load.Due(null, false, later);

        Assert.False(load.Due(8, false, later));
        Assert.False(load.Settled);
        Assert.True(load.Due(8, false, later + InGameAutoLoad.Settle));
    }

    [Fact]
    public void AGameClosedBeforeItSettledIsForgotten()
    {
        InGameAutoLoad load = new();
        load.Due(7, false, Start);
        load.Due(null, false, Start + TimeSpan.FromSeconds(5));

        Assert.False(load.Due(null, false, Start + InGameAutoLoad.Settle));
    }
}
