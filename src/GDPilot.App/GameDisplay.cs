using GDPilot.Core;
using GDPilot.Output;

namespace GDPilot.App;

/// <summary>Reads what the game is running at right now: its window size and its UI scale setting.</summary>
internal static class GameDisplay
{
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
        ScreenRect? game = GameWindow.ClientBounds(config.GameProcess);

        if (game is not ScreenRect bounds || !File.Exists(SettingsPath))
        {
            return null;
        }

        (int Width, int Height, double UiScale)? settings = GameSettings.Parse(File.ReadAllText(SettingsPath));

        if (settings is null)
        {
            return null;
        }

        return new LiveDisplay(bounds.Width, bounds.Height, settings.Value.UiScale);
    }
}
