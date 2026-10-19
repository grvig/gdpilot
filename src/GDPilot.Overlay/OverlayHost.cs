using System.Runtime.InteropServices;
using System.Windows.Threading;
using GDPilot.Core;

namespace GDPilot.Overlay;

/// <summary>
/// Runs the overlay on its own UI thread, so a slow redraw can never hold up
/// the input loop, and the input loop never has to be a UI thread itself.
/// </summary>
public sealed class OverlayHost : IDisposable
{
    private static readonly IntPtr TopmostSlot = new(-1);
    private const uint NoSize = 0x0001;
    private const uint NoActivate = 0x0010;

    private readonly Thread thread;
    private readonly ManualResetEventSlim ready = new();
    private OverlayWindow? window;

    public OverlayHost()
    {
        thread = new Thread(Run);
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        ready.Wait();
    }

    public IntPtr Handle { get; private set; }

    /// <summary>Shows the state in the chosen corner of the game, or of the screen when there is no game window.</summary>
    public void Update(OverlayState state, ScreenRect? game, OverlayCorner corner)
    {
        window?.Dispatcher.BeginInvoke(() => Apply(state, game, corner));
    }

    public void Dispose()
    {
        window?.Dispatcher.InvokeShutdown();
        thread.Join(TimeSpan.FromSeconds(2));
        ready.Dispose();
    }

    private void Run()
    {
        window = new OverlayWindow();
        window.Show();
        Handle = window.Handle;
        ready.Set();
        Dispatcher.Run();
    }

    private void Apply(OverlayState state, ScreenRect? game, OverlayCorner corner)
    {
        window!.Display(state);
        window.UpdateLayout();

        // Placed through Windows in physical pixels, because WPF's own Left and
        // Top are in scaled units while the game's bounds are physical.
        GetWindowRect(Handle, out Bounds own);
        ScreenRect area = new(0, 0, GetSystemMetrics(0), GetSystemMetrics(1));

        if (game is ScreenRect bounds)
        {
            area = bounds;
        }

        (int x, int y) = OverlayPlacement.Place(area, own.Right - own.Left, own.Bottom - own.Top, corner);

        // Re-asserted every update, since a borderless game can rise over a topmost window.
        SetWindowPos(Handle, TopmostSlot, x, y, 0, 0, NoSize | NoActivate);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Bounds
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr window, out Bounds bounds);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
