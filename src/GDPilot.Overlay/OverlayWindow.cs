using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using GDPilot.Core;

namespace GDPilot.Overlay;

/// <summary>
/// The heads-up card, drawn as one of the game's own panels: the mode as a gold
/// title over a gold rule, what is going on in amber, then each button in gold
/// with what it does in parchment. It must never take focus and never catch a
/// click, because either would land on the overlay instead of the game.
/// </summary>
public sealed class OverlayWindow : Window
{
    private readonly TextBlock mode = new()
    {
        FontFamily = GrimTheme.Serif,
        FontSize = 19,
        FontWeight = FontWeights.Bold,
        Foreground = GrimTheme.Gold,
    };

    private readonly TextBlock suspended = new()
    {
        FontFamily = GrimTheme.Serif,
        FontSize = 13,
        FontStyle = FontStyles.Italic,
        Foreground = GrimTheme.Warning,
    };

    private readonly StackPanel lines = new();
    private readonly FrameworkElement rule = GrimTheme.Rule();
    private IReadOnlyList<string>? shown;

    public OverlayWindow()
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

        StackPanel card = new() { MinWidth = 170 };
        card.Children.Add(mode);
        card.Children.Add(rule);
        card.Children.Add(suspended);
        card.Children.Add(lines);
        Content = GrimTheme.Panel(card, new Thickness(12, 7, 14, 9));

        SourceInitialized += (_, _) => ClickThrough.Apply(Handle);
    }

    public IntPtr Handle => new WindowInteropHelper(this).Handle;

    public void Display(OverlayState state)
    {
        mode.Text = state.Mode;

        if (state.Suspended is null)
        {
            suspended.Visibility = Visibility.Collapsed;
        }
        else
        {
            suspended.Text = state.Suspended;
            suspended.Visibility = Visibility.Visible;
        }

        IReadOnlyList<string> now = state.Shown;

        // Rebuilt only when the lines change, which is a mode change or a fold, not every redraw.
        if (shown is not null && shown.SequenceEqual(now))
        {
            return;
        }

        shown = now;
        lines.Children.Clear();

        for (int index = 0; index < now.Count; index++)
        {
            lines.Children.Add(Line(now[index], index < state.Statuses));
        }

        rule.Visibility = Visibility.Visible;

        if (now.Count == 0 && state.Suspended is null)
        {
            rule.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>A button's name in gold before what it does, or a status line in amber.</summary>
    private static TextBlock Line(string text, bool status)
    {
        TextBlock line = new()
        {
            FontFamily = GrimTheme.Serif,
            FontSize = 13,
            Foreground = GrimTheme.Parchment,
            Margin = new Thickness(0, 1, 0, 1),
        };

        if (status)
        {
            line.FontStyle = FontStyles.Italic;
            line.Foreground = GrimTheme.Gold;
            line.Text = text;
            return line;
        }

        int split = text.IndexOf(": ", StringComparison.Ordinal);

        if (split < 0)
        {
            line.Text = text;
            return line;
        }

        GrimTheme.AddButton(line, text[..split]);
        line.Inlines.Add(new Run("   " + text[(split + 2)..]));
        return line;
    }
}
