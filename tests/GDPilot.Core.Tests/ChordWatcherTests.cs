using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class ChordWatcherTests
{
    private const uint LeftControl = 0xA2;
    private const uint LeftAlt = 0xA4;
    private const uint Pause = 0x13;
    private const uint Cancel = 0x03;

    private readonly ChordWatcher watcher = new(KeyChord.Parse("Ctrl+Alt+Pause"));

    [Fact]
    public void FiresOnTheKeyThatCompletesTheChord()
    {
        Assert.False(watcher.Observe(LeftControl, true));
        Assert.False(watcher.Observe(LeftAlt, true));
        Assert.True(watcher.Observe(Pause, true));
    }

    [Fact]
    public void CancelIsReadAsPauseBecauseCtrlRenamesIt()
    {
        watcher.Observe(LeftControl, true);
        watcher.Observe(LeftAlt, true);

        Assert.True(watcher.Observe(Cancel, true));
    }

    [Fact]
    public void AReleasedModifierNoLongerCounts()
    {
        watcher.Observe(LeftControl, true);
        watcher.Observe(LeftAlt, true);
        watcher.Observe(LeftAlt, false);

        Assert.False(watcher.Observe(Pause, true));
    }

    [Fact]
    public void ReleasingTheKeyDoesNotFireAgain()
    {
        watcher.Observe(LeftControl, true);
        watcher.Observe(LeftAlt, true);
        watcher.Observe(Pause, true);

        Assert.False(watcher.Observe(Pause, false));
    }

    [Fact]
    public void GenericAndRightHandModifiersCountToo()
    {
        watcher.Observe(0x11, true);
        watcher.Observe(0xA5, true);

        Assert.True(watcher.Observe(Pause, true));
    }
}
