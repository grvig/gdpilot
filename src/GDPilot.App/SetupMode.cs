using System.Diagnostics;
using GDPilot.Core;
using GDPilot.Input;

namespace GDPilot.App;

/// <summary>--setup, and the first run by itself: says what is ready to play and what to do about what is not.</summary>
internal static class SetupMode
{
    internal static int Run(GDPilotConfig config)
    {
        foreach ((bool ready, string line) in FirstRun.Checks(Facts(config)))
        {
            if (ready)
            {
                Console.WriteLine($"  Ready: {line}");
            }
            else
            {
                Console.WriteLine($"  To do: {line}");
            }
        }

        return 0;
    }

    private static FirstRun.Facts Facts(GDPilotConfig config)
    {
        bool padConnected;

        using (SdlGamepadSource pad = new())
        {
            pad.Read();
            padConnected = pad.IsConnected;
        }

        Process? game = Process.GetProcessesByName(config.GameProcess).FirstOrDefault();
        bool running = game is not null;
        game?.Dispose();
        LiveDisplay? display = GameDisplay.Read(config);
        string? shown = null;

        if (display is LiveDisplay live)
        {
            shown = live.ProfileKey;
        }

        string? settings = null;

        if (File.Exists(GameDisplay.SettingsPath))
        {
            settings = File.ReadAllText(GameDisplay.SettingsPath);
        }

        return new FirstRun.Facts(padConnected, running, display is not null, shown, FunctionsFound(config), settings);
    }

    /// <summary>The check's own report of each name is for after a patch; here only its answer matters.</summary>
    private static bool FunctionsFound(GDPilotConfig config)
    {
        TextWriter output = Console.Out;
        TextWriter errors = Console.Error;
        Console.SetOut(TextWriter.Null);
        Console.SetError(TextWriter.Null);

        try
        {
            return CheckGameMode.Run(config, null) == 0;
        }
        finally
        {
            Console.SetOut(output);
            Console.SetError(errors);
        }
    }
}
