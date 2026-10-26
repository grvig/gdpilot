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
    private const string DllName = "GDPilot.InGame.dll";

    /// <summary>Every loaded copy's file name starts with this, whatever its version.</summary>
    private const string LoadedPrefix = "GDPilot.InGame.";

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

            if (GameLoader.IsLoaded(game, LoadedPrefix))
            {
                Console.WriteLine("The in-game part is already loaded. It stays until the game exits; restart the game to load a new build.");
                return 0;
            }

            string? problem = GameLoader.Load(game, Stage(Path.Combine(AppContext.BaseDirectory, DllName)));

            if (problem is not null)
            {
                Console.Error.WriteLine($"Could not load the in-game part: {problem}");
                return 1;
            }

            Console.WriteLine($"Loaded. What it found is in {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GDPilot", "in-game.log")}.");
            return 0;
        }
    }

    /// <summary>
    /// A copy of the DLL named after its contents, in GDPilot's own folder. The game
    /// keeps the file it loaded open until it exits, so loading the build output
    /// directly would stop the next build from replacing it.
    /// </summary>
    private static string Stage(string built)
    {
        byte[] bytes = File.ReadAllBytes(built);
        string version = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes))[..12].ToLowerInvariant();
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GDPilot", "in-game");
        string staged = Path.Combine(folder, $"{LoadedPrefix}{version}.dll");
        Directory.CreateDirectory(folder);

        if (!File.Exists(staged))
        {
            File.WriteAllBytes(staged, bytes);
        }

        return staged;
    }
}
