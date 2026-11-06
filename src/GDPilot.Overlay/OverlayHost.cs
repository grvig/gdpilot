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
    private static readonly IntPtr NotTopmostSlot = new(-2);
    private static readonly IntPtr TopSlot = IntPtr.Zero;
    private const uint NoSize = 0x0001;
    private const uint NoMove = 0x0002;
    private const uint NoOrder = 0x0004;
    private const uint NoActivate = 0x0010;
    private const uint WindowAbove = 3;
    private const int ExtendedStyle = -20;
    private const long TopmostStyle = 0x8;
    private const uint ForegroundChanged = 3;
    private const uint OutOfContext = 0;

    private readonly Thread thread;
    private readonly ManualResetEventSlim ready = new();
    private OverlayWindow? window;
    private WheelWindow? wheel;
    private MarkerWindow? marker;
    private PromptWindow? prompt;
    private object? cardBounds;

    /// <summary>Where the card is on screen, for reading the screen without it, or null before it is first placed.</summary>
    public ScreenRect? CardBounds => (ScreenRect?)Volatile.Read(ref cardBounds);

    // Where each window was last put, so one that has not moved costs nothing.
    // The stacking is re-asserted once a second and whenever another window
    // comes in front; doing it on every redraw made the desktop recompose fifty
    // times a second while the marker was up.
    private readonly Dictionary<IntPtr, (int X, int Y)> placed = new();
    private DateTime lastAsserted = DateTime.MinValue;
    private IntPtr gameWindow;
    private bool stackedOnTop;

    // Kept in a field, so the delegate Windows calls back is never collected.
    private WinEventProc? foregroundWatch;
    private IntPtr foregroundHook;

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
    /// <param name="gameHandle">The game's window, so the overlay can stack itself with it; zero when unknown.</param>
    public void Update(OverlayState state, ScreenRect? game, OverlayCorner corner, IntPtr gameHandle)
    {
        window?.Dispatcher.BeginInvoke(() =>
        {
            gameWindow = gameHandle;
            Apply(state, game, corner);
        });
    }

    public void Dispose()
    {
        if (foregroundHook != IntPtr.Zero)
        {
            window?.Dispatcher.Invoke(() => UnhookWinEvent(foregroundHook));
        }

        window?.Dispatcher.InvokeShutdown();
        thread.Join(TimeSpan.FromSeconds(2));
        ready.Dispose();
    }

    private void Run()
    {
        // Kept hidden until its first state is drawn and placed, or the empty card
        // would show for a moment in the middle of the screen, as it did live.
        window = new OverlayWindow();
        Handle = new System.Windows.Interop.WindowInteropHelper(window).EnsureHandle();
        wheel = new WheelWindow();
        marker = new MarkerWindow();
        prompt = new PromptWindow();

        // Told the moment another window comes in front, on this thread's own message loop,
        // so the overlay leaves the top layer before anything is drawn over it.
        foregroundWatch = (_, _, _, _, _, _, _) => Restack();
        foregroundHook = SetWinEventHook(ForegroundChanged, ForegroundChanged, IntPtr.Zero, foregroundWatch, 0, 0, OutOfContext);
        ready.Set();
        Dispatcher.Run();
    }

    private void Apply(OverlayState state, ScreenRect? game, OverlayCorner corner)
    {
        window!.Display(state);

        if (!window.IsVisible)
        {
            window.Opacity = 0;
            window.Show();
        }

        window.UpdateLayout();

        // Placed through Windows in physical pixels, because WPF's own Left and
        // Top are in scaled units while the game's bounds are physical.
        GetWindowRect(Handle, out Bounds own);
        ScreenRect area = new(0, 0, GetSystemMetrics(0), GetSystemMetrics(1));

        if (game is ScreenRect bounds)
        {
            area = bounds;
        }

        (int x, int y) = GameLayout.Place(area, own.Right - own.Left, own.Bottom - own.Top, corner, state.Showing);

        Place(Handle, x, y);
        Volatile.Write(ref cardBounds, new ScreenRect(x, y, own.Right - own.Left, own.Bottom - own.Top));
        window.Opacity = 1;
        ShowWheel(state.Wheel, area);
        ShowMarker(state.Marker);
        ShowPrompt(state.Prompt, area);

        // Restacked at once if the game has come in front or gone behind since, and once a second regardless.
        if (DateTime.UtcNow - lastAsserted > TimeSpan.FromSeconds(1) || GameInFront() != stackedOnTop)
        {
            lastAsserted = DateTime.UtcNow;
            Restack();
        }
    }

    /// <summary>
    /// Stacks the overlay with the game. While the game is in front, the overlay
    /// is topmost, since a borderless game would otherwise rise over it. Once any
    /// other window comes in front, the overlay leaves the top layer and sits just
    /// above the game: over the game, under everything over the game.
    /// </summary>
    private void Restack()
    {
        IntPtr[] windows = { Handle, wheel!.Handle, marker!.Handle, prompt!.Handle };
        stackedOnTop = GameInFront();

        if (stackedOnTop)
        {
            foreach (IntPtr each in windows)
            {
                SetWindowPos(each, TopmostSlot, 0, 0, 0, 0, NoMove | NoSize | NoActivate);
            }

            return;
        }

        IntPtr after = TopSlot;

        if (gameWindow != IntPtr.Zero)
        {
            IntPtr above = GetWindow(gameWindow, WindowAbove);

            while (above != IntPtr.Zero && windows.Contains(above))
            {
                above = GetWindow(above, WindowAbove);
            }

            // Below a topmost window the game heads the ordinary layer, and the top of it is just above the game.
            if (above != IntPtr.Zero && (GetWindowLongPtr(above, ExtendedStyle).ToInt64() & TopmostStyle) == 0)
            {
                after = above;
            }
        }

        foreach (IntPtr each in windows)
        {
            SetWindowPos(each, NotTopmostSlot, 0, 0, 0, 0, NoMove | NoSize | NoActivate);
            SetWindowPos(each, after, 0, 0, 0, 0, NoMove | NoSize | NoActivate);
        }
    }

    /// <summary>The prompt sits low in the middle of the game while something is in reach, and is hidden otherwise.</summary>
    private void ShowPrompt(string? text, ScreenRect area)
    {
        if (text is null)
        {
            if (prompt!.IsVisible)
            {
                prompt.Hide();
                placed.Remove(prompt.Handle);
            }

            return;
        }

        prompt!.Display(text);

        if (!prompt.IsVisible)
        {
            prompt.Show();
        }

        prompt.UpdateLayout();
        GetWindowRect(prompt.Handle, out Bounds own);
        Place(prompt.Handle, area.X + (area.Width - (own.Right - own.Left)) / 2, area.Y + (int)(area.Height * 0.78));
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

    private bool GameInFront()
    {
        return gameWindow != IntPtr.Zero && GetForegroundWindow() == gameWindow;
    }

    /// <summary>Moves a window, but only when it has moved; a window newly shown is stacked with the game.</summary>
    private void Place(IntPtr window, int x, int y)
    {
        if (placed.TryGetValue(window, out (int X, int Y) at) && at == (x, y))
        {
            return;
        }

        bool fresh = !placed.ContainsKey(window);
        placed[window] = (x, y);
        SetWindowPos(window, IntPtr.Zero, x, y, 0, 0, NoSize | NoActivate | NoOrder);

        if (fresh)
        {
            Restack();
        }
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

    private delegate void WinEventProc(IntPtr hook, uint eventType, IntPtr window, int objectId, int childId, uint thread, uint time);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr module, WinEventProc callback, uint process, uint thread, uint flags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr window, uint relation);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
}
