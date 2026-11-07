using System.Text;
using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class LoaderPlaceTests
{
    private static readonly byte[] Ours = Encoding.ASCII.GetBytes("MZ....GDPilot drop-in loader 1....");
    private static readonly byte[] Theirs = Encoding.ASCII.GetBytes("MZ....some other mod's dinput8....");

    [Fact]
    public void AnEmptyPlaceTakesTheLoader()
    {
        Assert.Equal(LoaderStep.Install, LoaderPlace.ToInstall(null));
    }

    [Fact]
    public void AnEarlierLoaderIsReplaced()
    {
        Assert.Equal(LoaderStep.Replace, LoaderPlace.ToInstall(Ours));
    }

    [Fact]
    public void AnotherModsFileIsNeverReplacedNorRemoved()
    {
        Assert.Equal(LoaderStep.Refuse, LoaderPlace.ToInstall(Theirs));
        Assert.Equal(LoaderStep.Refuse, LoaderPlace.ToRemove(Theirs));
    }

    [Fact]
    public void OnlyTheLoaderIsTakenAway()
    {
        Assert.Equal(LoaderStep.Remove, LoaderPlace.ToRemove(Ours));
        Assert.Equal(LoaderStep.Nothing, LoaderPlace.ToRemove(null));
    }
}
