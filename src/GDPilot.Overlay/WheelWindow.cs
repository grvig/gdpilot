using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using GDPilot.Core;

namespace GDPilot.Overlay;

/// <summary>
/// The skill wheel, drawn over the middle of the game while its button is held:
/// one disc per slot round a ring, the first at the top, the pointed one lit.
/// Click-through and never focused, like the corner card.
/// </summary>
public sealed class WheelWindow : Window
{
    private const double Size = 380;
    private const double Ring = 140;
    private const double Disc = 66;

    private static readonly Brush Idle = new SolidColorBrush(Color.FromArgb(200, 20, 20, 20));
    private static readonly Brush Lit = new SolidColorBrush(Color.FromArgb(235, 196, 150, 40));

    private readonly Canvas canvas = new() { Width = Size, Height = Size };
    private readonly List<Border> discs = new();
    private int shownSlots = -1;

    public WheelWindow()
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
        Content = canvas;
        SourceInitialized += (_, _) => ClickThrough.Apply(Handle);
    }

    public IntPtr Handle => new WindowInteropHelper(this).Handle;

    public void Display(WheelView wheel)
    {
        if (wheel.Labels.Count != shownSlots)
        {
            Build(wheel.Labels);
        }

        for (int slot = 0; slot < discs.Count; slot++)
        {
            discs[slot].Background = Idle;

            if (wheel.Selected == slot)
            {
                discs[slot].Background = Lit;
            }
        }
    }

    private void Build(IReadOnlyList<string> labels)
    {
        canvas.Children.Clear();
        discs.Clear();
        shownSlots = labels.Count;

        for (int slot = 0; slot < labels.Count; slot++)
        {
            double angle = 2 * Math.PI * slot / labels.Count;
            TextBlock text = new()
            {
                Text = labels[slot],
                Foreground = Brushes.White,
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Border disc = new()
            {
                Width = Disc,
                Height = Disc,
                CornerRadius = new CornerRadius(Disc / 2),
                BorderBrush = Brushes.Gainsboro,
                BorderThickness = new Thickness(2),
                Background = Idle,
                Child = text,
            };
            Canvas.SetLeft(disc, Size / 2 + Ring * Math.Sin(angle) - Disc / 2);
            Canvas.SetTop(disc, Size / 2 - Ring * Math.Cos(angle) - Disc / 2);
            canvas.Children.Add(disc);
            discs.Add(disc);
        }
    }
}
