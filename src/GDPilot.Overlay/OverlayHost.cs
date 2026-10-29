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
    private WheelWindow? wheel;
    private MarkerWindow? marker;

    // Where each window was last put, so one that has not moved costs nothing.
    // Being topmost is re-asserted once a second, since a borderless game can rise
    // over a topmost window; doing it on every redraw made the desktop recompose
    // fifty times a second while the marker was up.
    private readonly Dictionary<IntPtr, (int X, int Y)> placed = new();
    private DateTime lastAsserted = DateTime.MinValue;

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
        wheel = new WheelWindow();
        marker = new MarkerWindow();
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

        if (DateTime.UtcNow - lastAsserted > TimeSpan.FromSeconds(1))
        {
            placed.Clear();
            lastAsserted = DateTime.UtcNow;
        }

        Place(Handle, x, y);
        ShowWheel(state.Wheel, area);
        ShowMarker(state.Marker);
    }

    /// <summary>The marker is centred on the locked enemy, and hidden with nothing to mark.</summary>
    private void ShowMarker(LockMarker? view)
    {
        if (view is null)
        {
            if (marker!.IsVisible)
            {
                marker.Hide();
                placed.Remove(marker.Handle);
            }

            return;
        }

        marker!.Display(view);

        if (!marker.IsVisible)
        {
            marker.Show();
        }

        // The window's own units are scaled by the display; its physical size says by how much.
        GetWindowRect(marker.Handle, out Bounds own);
        double scale = (own.Right - own.Left) / marker.ActualWidth;
        int x = view.X - (int)Math.Round(MarkerWindow.CentreX * scale);
        int y = view.Y - (int)Math.Round(MarkerWindow.CentreY * scale);
        Place(marker.Handle, x, y);
    }

    /// <summary>Moves a window and keeps it on top, but only when it has moved or the once-a-second check is due.</summary>
    private void Place(IntPtr window, int x, int y)
    {
        if (placed.TryGetValue(window, out (int X, int Y) at) && at == (x, y))
        {
            return;
        }

        placed[window] = (x, y);
        SetWindowPos(window, TopmostSlot, x, y, 0, 0, NoSize | NoActivate);
    }

    /// <summary>The wheel sits over the middle of the game while open, and is hidden otherwise.</summary>
    private void ShowWheel(WheelView? view, ScreenRect area)
    {
        if (view is null)
        {
            if (wheel!.IsVisible)
            {
                wheel.Hide();
                placed.Remove(wheel.Handle);
            }

            return;
        }

        wheel!.Display(view);

        if (!wheel.IsVisible)
        {
            wheel.Show();
        }

        wheel.UpdateLayout();
        GetWindowRect(wheel.Handle, out Bounds own);
        int width = own.Right - own.Left;
        int height = own.Bottom - own.Top;
        Place(wheel.Handle, area.X + (area.Width - width) / 2, area.Y + (area.Height - height) / 2);
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
