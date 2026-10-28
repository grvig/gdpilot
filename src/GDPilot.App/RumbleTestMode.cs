using GDPilot.Input;

namespace GDPilot.App;

/// <summary>
/// --rumble-test: buzzes the pad's heavy motor, then its light one, so a player
/// can feel whether rumble works before relying on it in a fight.
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
        Console.WriteLine("Done. If nothing was felt, the pad or its driver has no rumble.");
        return 0;
    }
}
