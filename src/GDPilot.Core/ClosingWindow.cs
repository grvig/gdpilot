namespace GDPilot.Core;

/// <summary>
/// Started from Explorer, GDPilot has a console window of its own, which Windows
/// takes away the moment the program ends, along with the reason it stopped. So a
/// run that failed waits for a key, but only when nothing else shares the console:
/// from a command prompt the reason stays on screen by itself.
/// </summary>
public static class ClosingWindow
{
    public static bool ShouldWait(int exitCode, int programsOnConsole, bool redirected)
    {
        return exitCode != 0 && programsOnConsole == 1 && !redirected;
    }

    /// <summary>
    /// Whether an ordinary start, from Explorer or with Windows, gives up its window
    /// and lives in the tray. A first run keeps it, as its setup notes are there to be
    /// read, and so does anyone who started GDPilot from a command prompt.
    /// </summary>
    public static bool ShouldGoToTray(bool firstRun, bool plainRun, int programsOnConsole, bool redirected)
    {
        return !firstRun && plainRun && programsOnConsole == 1 && !redirected;
    }
}
