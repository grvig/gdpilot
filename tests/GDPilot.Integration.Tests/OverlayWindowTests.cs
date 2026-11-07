using System.Runtime.InteropServices;
using GDPilot.Core;
using GDPilot.Overlay;

namespace GDPilot.Integration.Tests;

/// <summary>
/// The overlay's drawing cannot be asserted, but the properties that keep it
/// out of the game's way can: Windows records them as extended window styles.
/// </summary>
public sealed class OverlayWindowTests
{
    private const long Topmost = 0x8;
    private const long Transparent = 0x20;
    private const long Layered = 0x80000;
    private const long NoActivate = 0x08000000;

    [Fact]
    public void TheOverlayIsClickThroughAndNeverTakesFocus()
    {
        ButtonLayers layers = BindingTable.Parse(new Dictionary<string, string> { ["A"] = "key:Space" }, 0.5);

        using OverlayHost overlay = new();
        overlay.Update(OverlayState.Build(Mode.Inventory, SuspendReason.None, layers), new ScreenRect(0, 0, 1280, 720), OverlayCorner.TopLeft, IntPtr.Zero);
        Thread.Sleep(300);

        long style = GetWindowLongPtr(overlay.Handle, -20).ToInt64();
        Assert.True((style & Transparent) != 0, "Clicks would land on the overlay.");
        Assert.True((style & NoActivate) != 0, "The overlay could take focus.");
        Assert.True((style & Layered) != 0, "The overlay is not layered.");
        Assert.NotEqual(overlay.Handle, GetForegroundWindow());
    }

    [Fact]
    public void AFaultOnTheOverlaysThreadIsCarriedPastAndTheOverlayKeepsDrawing()
    {
        ButtonLayers layers = BindingTable.Parse(new Dictionary<string, string> { ["A"] = "key:Space" }, 0.5);

        using OverlayHost overlay = new();
        overlay.Fault();
        Thread.Sleep(300);
        overlay.Update(OverlayState.Build(Mode.Inventory, SuspendReason.None, layers), new ScreenRect(0, 0, 1280, 720), OverlayCorner.TopLeft, IntPtr.Zero);
        Thread.Sleep(300);

        Assert.Equal(1, overlay.Faults);
        Assert.True(IsWindowVisible(overlay.Handle), "The overlay stopped drawing after the fault.");
    }

    [Fact]
    public void TheOverlayIsTopmostOnlyWhileTheGameIsInFront()
    {
        ButtonLayers layers = BindingTable.Parse(new Dictionary<string, string> { ["A"] = "key:Space" }, 0.5);
        OverlayState state = OverlayState.Build(Mode.Combat, SuspendReason.None, layers);

        using OverlayHost overlay = new();

        // Whatever is in front stands in for the game; then a window that is not in front does.
        overlay.Update(state, new ScreenRect(0, 0, 1280, 720), OverlayCorner.TopLeft, GetForegroundWindow());
        Assert.True(SpinWait.SpinUntil(() => IsTopmost(overlay.Handle), 2000), "Over the game in front, the overlay should be on top.");

        overlay.Update(state, new ScreenRect(0, 0, 1280, 720), OverlayCorner.TopLeft, GetDesktopWindow());
        Assert.True(SpinWait.SpinUntil(() => !IsTopmost(overlay.Handle), 2000), "With the game behind other windows, the overlay should not float over them.");
    }

    private static bool IsTopmost(IntPtr window)
    {
        return (GetWindowLongPtr(window, -20).ToInt64() & Topmost) != 0;
    }

    [Fact]
    public void TheOverlayOpensAndClosesRepeatedly()
    {
        for (int round = 0; round < 3; round++)
        {
            using OverlayHost overlay = new();
            Assert.NotEqual(IntPtr.Zero, overlay.Handle);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();
}
