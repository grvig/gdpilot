using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GDPilot.Output;

/// <summary>
/// Answers whether a named process owns the window in front. The focus watcher
/// polls this rather than hooking focus events, because a poll cannot miss an
/// event and the cost of one call every few hundred milliseconds is nothing.
/// </summary>
public static class ForegroundProbe
{
    public static bool IsInFront(string processName)
    {
        IntPtr window = GetForegroundWindow();

        if (window == IntPtr.Zero)
        {
            return false;
        }

        GetWindowThreadProcessId(window, out int processId);

        // The owner can exit between the two calls, and some processes refuse to
        // be opened. Either way the game is not reliably in front.
        try
        {
            using Process owner = Process.GetProcessById(processId);
            return string.Equals(owner.ProcessName, processName, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowThreadProcessId(IntPtr window, out int processId);
}
