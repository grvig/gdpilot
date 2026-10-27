using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace GDPilot.Overlay;

/// <summary>
/// The interact prompt, such as "A  Pick up", in a small card low in the middle
/// of the game while something is in reach. Click-through and never focused,
/// like the corner card.
/// </summary>
public sealed class PromptWindow : Window
{
    private readonly TextBlock text = new() { Foreground = Brushes.White, FontSize = 18, FontWeight = FontWeights.SemiBold };

    public PromptWindow()
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
        Content = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(200, 16, 16, 16)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(160, 250, 240, 215)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(14, 6, 14, 6),
            Child = text,
        };
        SourceInitialized += (_, _) => ClickThrough.Apply(Handle);
    }

    public IntPtr Handle => new WindowInteropHelper(this).Handle;

    public void Display(string prompt)
    {
        text.Text = prompt;
    }
}
