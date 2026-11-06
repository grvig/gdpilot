using GDPilot.Core;
using GDPilot.Output;

namespace GDPilot.App;

/// <summary>Reads what the game is running at right now: its window size and its UI scale setting.</summary>
internal static class GameDisplay
{
    private static DateTime settingsWritten;
    private static double uiScale;

    internal static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "My Games",
        "Grim Dawn",
        "Settings",
        "options.txt");

    /// <summary>
    /// The live display, or null when the game is not running or its settings
    /// cannot be read. Size comes from the window itself, which is the truth;
    /// UI scale has no other source than the settings file.
    /// </summary>
    internal static LiveDisplay? Read(GDPilotConfig config)
    {
        return Read(GameWindow.ClientBounds(config.GameProcess));
    }

    /// <summary>The same, for a window already found.</summary>
    internal static LiveDisplay? Read(ScreenRect? game)
    {
        if (game is not ScreenRect bounds || !File.Exists(SettingsPath))
        {
            return null;
        }

        // This is asked several times a second, and the file seldom changes.
        DateTime written = File.GetLastWriteTimeUtc(SettingsPath);

        if (written != settingsWritten)
        {
            try
            {
                (int Width, int Height, double UiScale)? settings = GameSettings.Parse(File.ReadAllText(SettingsPath));

                if (settings is null)
                {
                    return null;
                }

                uiScale = settings.Value.UiScale;
                settingsWritten = written;
            }
            catch (IOException)
            {
                // The game may be writing it this moment; the next look will do.
                return null;
            }
        }

        return new LiveDisplay(bounds.Width, bounds.Height, uiScale, bounds.X, bounds.Y);
    }
}
