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
}
