using System.Diagnostics;
using System.Runtime.InteropServices;
using GDPilot.Core;

namespace GDPilot.Output;

/// <summary>Finds where the game is drawing, so the overlay can sit in one of its corners.</summary>
public static class GameWindow
{
    /// <summary>The game's client area in physical pixels, or null if it is not running or is minimised.</summary>
    public static ScreenRect? ClientBounds(string processName)
    {
        foreach (Process process in Process.GetProcessesByName(processName))
        {
            using (process)
            {
                IntPtr window = process.MainWindowHandle;

                if (window == IntPtr.Zero || IsIconic(window) || !GetClientRect(window, out Bounds client))
                {
                    continue;
                }

                // The client rectangle is window-relative; its origin has to be placed on the screen.
                Point origin = new();

                if (!ClientToScreen(window, ref origin))
                {
                    continue;
                }

                return new ScreenRect(origin.X, origin.Y, client.Right - client.Left, client.Bottom - client.Top);
            }
        }

        return null;
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
    private static extern bool GetClientRect(IntPtr window, out Bounds bounds);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr window, ref Point point);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr window);
}
