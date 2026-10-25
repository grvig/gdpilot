using System.Diagnostics;
using System.Text.RegularExpressions;
using GDPilot.Core;
using Microsoft.Win32;

namespace GDPilot.App;

/// <summary>
/// --check-game [folder]: reads the installed game's DLLs and reports whether
/// every function the in-game part needs is still there by name. The game
/// does not need to be running. It is the first thing to run after a patch.
/// </summary>
internal static class CheckGameMode
{
    internal static int Run(GDPilotConfig config, string? folder)
    {
        folder ??= FindGame(config);

        if (folder is null || !File.Exists(Path.Combine(folder, "Game.dll")))
        {
            Console.Error.WriteLine("Could not find Grim Dawn's x64 folder. Give it: --check-game \"D:\\...\\Grim Dawn\\x64\"");
            return 2;
        }

        string list = Path.Combine(AppContext.BaseDirectory, "game-functions.txt");
        Dictionary<string, ExportTable> tables = new(StringComparer.OrdinalIgnoreCase);
        int missing = 0;
        int found = 0;

        Console.WriteLine($"Checking {folder}");

        foreach (string line in File.ReadAllLines(list))
        {
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            string[] parts = line.Split(' ', 2);

            if (!tables.TryGetValue(parts[0], out ExportTable? table))
            {
                table = ExportTable.Read(File.ReadAllBytes(Path.Combine(folder, parts[0])));
                tables[parts[0]] = table;
            }

            if (table.Names.ContainsKey(parts[1]))
            {
                found++;
            }
            else
            {
                missing++;
                Console.WriteLine($"  Missing from {parts[0]}: {parts[1]}");
            }
        }

        if (missing > 0)
        {
            Console.WriteLine($"{missing} of {found + missing} functions are missing. The in-game part would stay off; everything else still works.");
            return 1;
        }

        Console.WriteLine($"All {found} functions are present. The in-game part can find everything it needs.");
        return 0;
    }

    /// <summary>The running game's folder, or Steam's install of it.</summary>
    private static string? FindGame(GDPilotConfig config)
    {
        foreach (Process process in Process.GetProcessesByName(config.GameProcess))
        {
            using (process)
            {
                try
                {
                    return Path.GetDirectoryName(process.MainModule?.FileName);
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // Another user's process cannot be inspected; fall through to Steam.
                }
            }
        }

        string? steam = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;

        if (steam is null)
        {
            return null;
        }

        string libraries = Path.Combine(steam, "steamapps", "libraryfolders.vdf");

        if (!File.Exists(libraries))
        {
            return null;
        }

        foreach (Match library in Regex.Matches(File.ReadAllText(libraries), "\"path\"\\s+\"([^\"]+)\""))
        {
            string candidate = Path.Combine(library.Groups[1].Value.Replace(@"\\", @"\"), "steamapps", "common", "Grim Dawn", "x64");

            if (File.Exists(Path.Combine(candidate, "Game.dll")))
            {
                return candidate;
            }
        }

        return null;
    }
}
