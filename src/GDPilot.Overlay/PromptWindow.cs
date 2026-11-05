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
    private readonly TextBlock text = new() { Foreground = GrimTheme.Parchment, FontFamily = GrimTheme.Serif, FontSize = 17 };

    public PromptWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        // Stacked by the host with the game, not always on top of everything.
        Topmost = false;
        ShowActivated = false;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        IsHitTestVisible = false;
        // As the game labels an item on the ground: a small dark plate with a bronze edge.
        Content = GrimTheme.Panel(text, new Thickness(12, 3, 12, 4));
        SourceInitialized += (_, _) => ClickThrough.Apply(Handle);
    }

    public IntPtr Handle => new WindowInteropHelper(this).Handle;

    /// <summary>The button in gold, then what it would do, as "A  Pick up".</summary>
    public void Display(string prompt)
    {
        text.Inlines.Clear();
        int split = prompt.IndexOf("  ", StringComparison.Ordinal);

        if (split < 0)
        {
            text.Inlines.Add(new System.Windows.Documents.Run(prompt));
            return;
        }

        GrimTheme.AddButton(text, prompt[..split]);
        text.Inlines.Add(new System.Windows.Documents.Run("  " + prompt[(split + 2)..]));
    }
}
