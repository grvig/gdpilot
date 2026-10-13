using GDPilot.Core;
using GDPilot.Output;

namespace GDPilot.Integration.Tests;

/// <summary>
/// Runs inside the loopback collection so the injected chord is swallowed after
/// the kill switch hook has seen it, and never reaches whatever window has focus.
/// </summary>
[Collection(LoopbackCollection.Name)]
public sealed class KillSwitchHookLoopbackTests
{
    private static readonly KeyChord Chord = KeyChord.Parse("Ctrl+Alt+Pause");

    private readonly KeySender keys = new(KeyInjectionMode.VirtualKey);

    public KillSwitchHookLoopbackTests(LoopbackFixture loopback)
    {
        // Taken only to make sure the loopback window is running first.
        _ = loopback;
    }

    [Fact]
    public void PressingTheChordFiresTheKillSwitch()
    {
        using ManualResetEventSlim fired = new();
        using KillSwitchHook hook = new(Chord, () => fired.Set());

        Press(KeyCode.LeftControl, KeyCode.LeftAlt, KeyCode.Pause);

        Assert.True(fired.Wait(TimeSpan.FromSeconds(2)), "The kill switch did not fire.");
    }

    [Fact]
    public void PauseWithoutTheModifiersDoesNotFire()
    {
        using ManualResetEventSlim fired = new();
        using KillSwitchHook hook = new(Chord, () => fired.Set());

        Press(KeyCode.LeftControl, KeyCode.Pause);

        Assert.False(fired.Wait(TimeSpan.FromMilliseconds(400)), "The kill switch fired without Alt.");
    }

    /// <summary>Holds each key in order, then lets them all go in reverse.</summary>
    private void Press(params KeyCode[] chord)
    {
        foreach (KeyCode key in chord)
        {
            keys.Down(key);
        }

        foreach (KeyCode key in chord.Reverse())
        {
            keys.Up(key);
        }
    }
}
