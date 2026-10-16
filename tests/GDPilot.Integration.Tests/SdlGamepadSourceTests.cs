using GDPilot.Core;
using GDPilot.Input;

namespace GDPilot.Integration.Tests;

/// <summary>
/// Runs with or without a pad attached. It proves the native SDL library loads
/// and starts on this machine, which is the part a missing or mismatched native
/// binary would break. Reading real buttons needs hands on a pad.
/// </summary>
public sealed class SdlGamepadSourceTests
{
    [Fact]
    public void SdlStartsAndReadsWhateverIsConnected()
    {
        using SdlGamepadSource source = new();

        GamepadSnapshot snapshot = source.Read();

        if (!source.IsConnected)
        {
            Assert.Equal(GamepadSnapshot.Rest, snapshot);
        }

        Assert.InRange(snapshot.LeftStick.Magnitude, 0, Math.Sqrt(2));
        Assert.InRange(snapshot.LeftTrigger, 0, 1);
    }

    [Fact]
    public void ReadingRepeatedlyWithoutAPadStaysAtRest()
    {
        using SdlGamepadSource source = new();

        for (int poll = 0; poll < 5; poll++)
        {
            GamepadSnapshot snapshot = source.Read();

            if (!source.IsConnected)
            {
                Assert.Equal(GamepadSnapshot.Rest, snapshot);
            }
        }
    }
}
