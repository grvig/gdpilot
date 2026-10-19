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
    public void TheOverlayIsClickThroughTopmostAndNeverTakesFocus()
    {
        ButtonLayers layers = BindingTable.Parse(new Dictionary<string, string> { ["A"] = "key:Space" }, 0.5);

        using OverlayHost overlay = new();
        overlay.Update(OverlayState.Build(Mode.Inventory, SuspendReason.None, layers), new ScreenRect(0, 0, 1280, 720), OverlayCorner.TopLeft);
        Thread.Sleep(300);

        long style = GetWindowLongPtr(overlay.Handle, -20).ToInt64();
        Assert.True((style & Transparent) != 0, "Clicks would land on the overlay.");
        Assert.True((style & NoActivate) != 0, "The overlay could take focus.");
        Assert.True((style & Layered) != 0, "The overlay is not layered.");
        Assert.True((style & Topmost) != 0, "The overlay is not topmost.");
        Assert.NotEqual(overlay.Handle, GetForegroundWindow());
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
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
}
