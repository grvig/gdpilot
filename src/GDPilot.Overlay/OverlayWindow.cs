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
        card.Background = new SolidColorBrush(Color.FromArgb(220, 16, 16, 16));
        card.CornerRadius = new CornerRadius(6);
        card.Padding = new Thickness(10);
        card.Child = lines;
        Content = card;

        SourceInitialized += (_, _) => ClickThrough.Apply(Handle);
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
}
