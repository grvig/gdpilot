using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using GDPilot.Core;

namespace GDPilot.Overlay;

/// <summary>
/// The heads-up display. It must never take focus and never catch a click,
/// because either would land on the overlay instead of the game it sits over.
/// </summary>
public sealed class OverlayWindow : Window
{
    private const int ExtendedStyle = -20;
    private const long Transparent = 0x20;
    private const long ToolWindow = 0x80;
    private const long Layered = 0x80000;
    private const long NoActivate = 0x08000000;

    private readonly TextBlock mode = new();
    private readonly TextBlock suspended = new();
    private readonly TextBlock legend = new();

    public OverlayWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowActivated = false;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        IsHitTestVisible = false;

        mode.FontSize = 20;
        mode.FontWeight = FontWeights.Bold;
        mode.Foreground = Brushes.White;
        suspended.FontSize = 14;
        suspended.Foreground = Brushes.OrangeRed;
        legend.FontSize = 12;
        legend.Foreground = Brushes.Gainsboro;

        StackPanel lines = new();
        lines.Children.Add(mode);
        lines.Children.Add(suspended);
        lines.Children.Add(legend);

        Border card = new();
        card.Background = new SolidColorBrush(Color.FromArgb(170, 16, 16, 16));
        card.CornerRadius = new CornerRadius(6);
        card.Padding = new Thickness(10);
        card.Child = lines;
        Content = card;

        SourceInitialized += (_, _) => MakeClickThrough();
    }

    public IntPtr Handle => new WindowInteropHelper(this).Handle;

    public void Display(OverlayState state)
    {
        mode.Text = state.Mode;
        legend.Text = string.Join(Environment.NewLine, state.Legend);

        if (state.Suspended is null)
        {
            suspended.Visibility = Visibility.Collapsed;
            return;
        }

        suspended.Text = state.Suspended;
        suspended.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// WPF can make a window see-through but not click-through. The extended
    /// styles below are what make Windows pass clicks and focus to whatever is
    /// underneath, which here is the game.
    /// </summary>
    private void MakeClickThrough()
    {
        long style = GetWindowLongPtr(Handle, ExtendedStyle).ToInt64();
        SetWindowLongPtr(Handle, ExtendedStyle, new IntPtr(style | Transparent | ToolWindow | Layered | NoActivate));
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);
}
