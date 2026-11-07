using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GDPilot.Output;

/// <summary>
/// Answers whether a named process owns the window in front. The focus watcher
/// polls this rather than hooking focus events, because a poll cannot miss an
/// event. It is asked every tick, and naming a window's process takes a snapshot
/// of every process on the machine, which measured at about a tenth of a core
/// idle on 2026-11-08; so the answer is kept for as long as the same window of the
/// same process stays in front. The process is asked for each time, as that costs
/// nothing, because Windows hands a closed window's number on to new windows.
/// </summary>
public static class ForegroundProbe
{
    private static readonly object Gate = new();
    private static IntPtr lastWindow;
    private static int lastProcess;
    private static string lastName = string.Empty;
    private static bool lastAnswer;

    public static bool IsInFront(string processName)
    {
        IntPtr window = GetForegroundWindow();

        if (window == IntPtr.Zero)
        {
            return false;
        }

        GetWindowThreadProcessId(window, out int processId);

        lock (Gate)
        {
            if (window == lastWindow && processId == lastProcess && processName == lastName)
            {
                return lastAnswer;
            }

            bool answer = Owns(processId, processName);
            lastWindow = window;
            lastProcess = processId;
            lastName = processName;
            lastAnswer = answer;
            return answer;
        }
    }

    private static bool Owns(int processId, string processName)
    {
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
