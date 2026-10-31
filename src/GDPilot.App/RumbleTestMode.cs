using GDPilot.Core;
using GDPilot.Input;

namespace GDPilot.App;

/// <summary>
/// --rumble-test: buzzes the pad's heavy motor, then its light one, so a player
/// can feel whether rumble works before relying on it in a fight; then plays
/// what a fight feels like: a few wounding ticks, a heavy blow, a kill, and a
/// lock taken.
/// </summary>
internal static class RumbleTestMode
{
    internal static int Run()
    {
        using SdlGamepadSource pad = new();
        pad.Read();

        if (!pad.IsConnected)
        {
            Console.Error.WriteLine("No pad is connected.");
            return 1;
        }

        Console.WriteLine($"Pad: {pad.Name}. Heavy motor, then light motor.");
        pad.Rumble(0.8, 0, TimeSpan.FromMilliseconds(500));
        Thread.Sleep(800);
        pad.Rumble(0, 0.8, TimeSpan.FromMilliseconds(500));
        Thread.Sleep(800);
        Console.WriteLine("A fight: wounds, a heavy blow, a kill, then a lock taken.");
        Play(pad, new CombatRumble());
        Thread.Sleep(400);

        if (RumblePolicy.ForLock(LockChange.Locked) is Rumble locked)
        {
            pad.Rumble(locked.Heavy, locked.Light, locked.Length);
            Thread.Sleep(300);
        }

        Console.WriteLine("Done. If nothing was felt, the pad or its driver has no rumble.");
        return 0;
    }

    /// <summary>An enemy close by, wounded a few times, struck hard, then killed, through the same rules a fight uses.</summary>
    private static void Play(SdlGamepadSource pad, CombatRumble fight)
    {
        int[] life = { 100, 96, 92, 88, 84, 60, 20 };

        foreach (int health in life)
        {
            Feel(pad, fight.Update(new[] { new SeenTarget(1, 0.5, 0.4, 4, health, true) }, null, TimeSpan.FromMilliseconds(150)));
            Thread.Sleep(150);
        }

        Feel(pad, fight.Update(Array.Empty<SeenTarget>(), null, TimeSpan.FromMilliseconds(150)));
    }

    private static void Feel(SdlGamepadSource pad, Rumble? rumble)
    {
        if (rumble is Rumble felt)
        {
            pad.Rumble(felt.Heavy, felt.Light, felt.Length);
        }
    }
}
