using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GDPilot.Overlay;

/// <summary>
/// The look of Grim Dawn's own panels, so the overlay reads as part of the game:
/// near-black stone behind, a worn bronze edge with a thin gold line inside it,
/// gold serif titles and parchment text. Book Antiqua stands in for the game's
/// own serif, which ships only inside its archives; Windows has carried Book
/// Antiqua since Office 97 and falls back to another serif without it.
/// </summary>
internal static class GrimTheme
{
    internal static readonly FontFamily Serif = new("Book Antiqua, Palatino Linotype, Georgia, serif");

    internal static readonly Brush Stone = Frozen(Color.FromArgb(232, 14, 12, 10));
    internal static readonly Brush StoneLight = Frozen(Color.FromArgb(232, 30, 25, 20));
    internal static readonly Brush Bronze = Frozen(Color.FromArgb(255, 112, 82, 44));
    internal static readonly Brush GoldLine = Frozen(Color.FromArgb(150, 201, 164, 92));
    internal static readonly Brush Gold = Frozen(Color.FromArgb(255, 222, 181, 96));
    internal static readonly Brush Parchment = Frozen(Color.FromArgb(255, 216, 203, 176));
    internal static readonly Brush Faded = Frozen(Color.FromArgb(255, 158, 146, 122));
    internal static readonly Brush Warning = Frozen(Color.FromArgb(255, 214, 96, 52));
    internal static readonly Brush Blood = Frozen(Color.FromArgb(240, 168, 30, 26));

    /// <summary>A panel in the game's manner: bronze edge, gold inner line, dark stone fading lighter at the top.</summary>
    internal static Border Panel(UIElement content, Thickness padding)
    {
        LinearGradientBrush stone = new(Color.FromArgb(236, 34, 28, 22), Color.FromArgb(236, 12, 10, 9), 90);
        stone.Freeze();
        Border inner = new()
        {
            BorderBrush = GoldLine,
            BorderThickness = new Thickness(1),
            Background = stone,
            Padding = padding,
            Child = content,
        };

        return new Border
        {
            BorderBrush = Bronze,
            BorderThickness = new Thickness(2),
            Padding = new Thickness(1),
            Background = Stone,
            Child = inner,
        };
    }

    /// <summary>The thin gold rule the game draws under a panel's title, fading at both ends.</summary>
    internal static FrameworkElement Rule()
    {
        LinearGradientBrush fade = new() { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
        fade.GradientStops.Add(new GradientStop(Color.FromArgb(0, 201, 164, 92), 0));
        fade.GradientStops.Add(new GradientStop(Color.FromArgb(200, 201, 164, 92), 0.5));
        fade.GradientStops.Add(new GradientStop(Color.FromArgb(0, 201, 164, 92), 1));
        fade.Freeze();
        return new Border { Height = 1, Background = fade, Margin = new Thickness(0, 3, 0, 5) };
    }

    private static Brush Frozen(Color color)
    {
        SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }
}
