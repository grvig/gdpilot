using System.Runtime.InteropServices;

namespace GDPilot.Overlay;

/// <summary>
/// WPF can make a window see-through but not click-through. These extended
/// styles are what make Windows pass clicks and focus to whatever is
/// underneath, which here is the game.
/// </summary>
internal static class ClickThrough
{
    private const int ExtendedStyle = -20;
    private const long Transparent = 0x20;
    private const long ToolWindow = 0x80;
    private const long Layered = 0x80000;
    private const long NoActivate = 0x08000000;

    internal static void Apply(IntPtr window)
    {
        long style = GetWindowLongPtr(window, ExtendedStyle).ToInt64();
        SetWindowLongPtr(window, ExtendedStyle, new IntPtr(style | Transparent | ToolWindow | Layered | NoActivate));
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);
}
