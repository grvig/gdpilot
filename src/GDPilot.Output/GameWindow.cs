using System.Diagnostics;
using System.Runtime.InteropServices;
using GDPilot.Core;

namespace GDPilot.Output;

/// <summary>
/// Finds where the game is drawing, so the overlay can sit in one of its corners.
/// Asked many times a second, so the window once found is kept while it lasts and shows,
/// and a game not running is looked for again only once a second: finding a
/// process by name takes a snapshot of every process on the machine.
/// </summary>
public static class GameWindow
{
    private static readonly TimeSpan LookAgain = TimeSpan.FromSeconds(1);
    private static readonly object Gate = new();
    private static readonly Stopwatch SinceLook = new();
    private static IntPtr found;
    private static uint foundProcess;
    private static string foundName = string.Empty;

    /// <summary>The game's main window, or zero if it is not running.</summary>
    public static IntPtr Handle(string processName)
    {
        lock (Gate)
        {
            if (found != IntPtr.Zero && processName == foundName && IsWindow(found) && IsWindowVisible(found) && GetWindowThreadProcessId(found, out uint owner) != 0 && owner == foundProcess)
            {
                return found;
            }

            found = IntPtr.Zero;

            if (SinceLook.IsRunning && SinceLook.Elapsed < LookAgain && processName == foundName)
            {
                return IntPtr.Zero;
            }

            SinceLook.Restart();
            foundName = processName;

            foreach (Process process in Process.GetProcessesByName(processName))
            {
                using (process)
                {
                    if (process.MainWindowHandle != IntPtr.Zero && found == IntPtr.Zero)
                    {
                        found = process.MainWindowHandle;
                        foundProcess = (uint)process.Id;
                    }
                }
            }

            return found;
        }
    }

    /// <summary>Which run of the game a window belongs to.</summary>
    public static int ProcessId(IntPtr window)
    {
        GetWindowThreadProcessId(window, out uint id);
        return (int)id;
    }

    /// <summary>The game's client area in physical pixels, or null if it is not running or is minimised.</summary>
    public static ScreenRect? ClientBounds(string processName)
    {
        IntPtr window = Handle(processName);

        if (window == IntPtr.Zero || IsIconic(window) || !GetClientRect(window, out Bounds client))
        {
            return null;
        }

        // The client rectangle is window-relative; its origin has to be placed on the screen.
        Point origin = new();

        if (!ClientToScreen(window, ref origin))
        {
            return null;
        }

        return new ScreenRect(origin.X, origin.Y, client.Right - client.Left, client.Bottom - client.Top);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Bounds
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        internal int X;
        internal int Y;
    }

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr window, out Bounds bounds);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr window, ref Point point);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr window);
}
