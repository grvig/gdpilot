using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class PadWatchTests
{
    [Fact]
    public void TheFirstLookIsNoChangeWhateverItSees()
    {
        Assert.Equal(PadChange.None, new PadWatch().Changed(true, "Xbox 360 Controller"));
        Assert.Equal(PadChange.None, new PadWatch().Changed(false, null));
    }

    [Fact]
    public void APadPulledOutIsLostAndPluggedBackIsFound()
    {
        PadWatch watch = new();
        watch.Changed(true, "Xbox 360 Controller");

        Assert.Equal(PadChange.Lost, watch.Changed(false, null));
        Assert.Equal(PadChange.None, watch.Changed(false, null));
        Assert.Equal(PadChange.Found, watch.Changed(true, "Xbox 360 Controller"));
        Assert.Equal(PadChange.None, watch.Changed(true, "Xbox 360 Controller"));
    }

    [Fact]
    public void ThePadThatWentIsKnownWhenItComesBack()
    {
        PadWatch watch = new();
        watch.Changed(true, "Xbox 360 Controller");
        watch.Changed(false, null);
        watch.Changed(true, "Xbox 360 Controller");

        Assert.True(watch.SameAsBefore("Xbox 360 Controller"));
    }

    [Fact]
    public void AnotherPadIsNot()
    {
        PadWatch watch = new();
        watch.Changed(true, "Xbox 360 Controller");
        watch.Changed(false, null);
        watch.Changed(true, "DualSense Wireless Controller");

        Assert.False(watch.SameAsBefore("DualSense Wireless Controller"));
    }

    [Fact]
    public void APadFirstPluggedInAfterStartIsFoundAndIsNotTheSameAsNone()
    {
        PadWatch watch = new();
        watch.Changed(false, null);

        Assert.Equal(PadChange.Found, watch.Changed(true, "Xbox 360 Controller"));
        Assert.False(watch.SameAsBefore("Xbox 360 Controller"));
    }
}
