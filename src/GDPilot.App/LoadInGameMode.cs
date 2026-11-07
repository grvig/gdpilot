using System.Diagnostics;
using GDPilot.Core;
using GDPilot.Output;

namespace GDPilot.App;

/// <summary>
/// --load-in-game: checks the installed game still has every function the
/// in-game part needs, then loads GDPilot.InGame.dll into the running game. The
/// in-game part writes what it found to %LOCALAPPDATA%\GDPilot\in-game.log.
/// </summary>
internal static class LoadInGameMode
{
    internal static int Run(GDPilotConfig config)
    {
        Process? game = Process.GetProcessesByName(config.GameProcess).FirstOrDefault();

        if (game is null)
        {
            Console.Error.WriteLine($"{config.GameProcess} is not running. Start it first.");
            return 1;
        }

        using (game)
        {
            if (CheckGameMode.Run(config, null) != 0)
            {
                return 1;
            }

            if (GameLoader.IsLoaded(game, InGamePart.LoadedPrefix))
            {
                Console.WriteLine("The in-game part is already loaded. It stays until the game exits; restart the game to load a new build.");
                return 0;
            }

            if (InGamePart.Stage() is not string staged)
            {
                Console.Error.WriteLine("GDPilot.InGame.dll is missing from beside GDPilot.exe.");
                return 1;
            }

            string? problem = GameLoader.Load(game, staged);

            if (problem is not null)
            {
                Console.Error.WriteLine($"Could not load the in-game part: {problem}");
                return 1;
            }

            Console.WriteLine($"Loaded. What it found is in {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GDPilot", "in-game.log")}.");
            return 0;
        }
    }
}
