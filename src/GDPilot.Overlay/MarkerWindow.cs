using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using GDPilot.Core;

namespace GDPilot.Overlay;

/// <summary>
/// The lock-on marker: a ring round a dot, centred on the locked enemy, with a
/// thin bar of its life beneath. Click-through and never focused, like the
/// corner card, so it never takes a click meant for the game.
/// </summary>
public sealed class MarkerWindow : Window
{
    private const double Across = 64;
    private const double Tall = 76;
    private const double Ring = 28;
    private const double Dot = 8;
    private const double BarWidth = 44;
    private const double BarHeight = 5;

    /// <summary>Where the ring's centre sits, from the window's top left, in the window's own units.</summary>
    public const double CentreX = Across / 2;
    public const double CentreY = 30;

    private readonly Rectangle life = new() { Height = BarHeight, Fill = new SolidColorBrush(Color.FromArgb(230, 200, 40, 40)) };
    private int shownHealth = -1;

    public MarkerWindow()
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
        Content = Draw();
        SourceInitialized += (_, _) => ClickThrough.Apply(Handle);
    }

    public IntPtr Handle => new WindowInteropHelper(this).Handle;

    public void Display(LockMarker marker)
    {
        if (marker.Health == shownHealth)
        {
            return;
        }

        shownHealth = marker.Health;
        life.Width = BarWidth * Math.Clamp(marker.Health, 0, 100) / 100;
    }

    private Canvas Draw()
    {
        Canvas canvas = new() { Width = Across, Height = Tall };
        Brush light = new SolidColorBrush(Color.FromArgb(235, 250, 240, 215));
        Ellipse ring = new() { Width = Ring, Height = Ring, Stroke = light, StrokeThickness = 2 };
        Ellipse dot = new() { Width = Dot, Height = Dot, Fill = light };
        Rectangle track = new() { Width = BarWidth, Height = BarHeight, Fill = new SolidColorBrush(Color.FromArgb(170, 15, 15, 15)) };
        Place(canvas, ring, CentreX - Ring / 2, CentreY - Ring / 2);
        Place(canvas, dot, CentreX - Dot / 2, CentreY - Dot / 2);
        Place(canvas, track, CentreX - BarWidth / 2, CentreY + Ring / 2 + 8);
        Place(canvas, life, CentreX - BarWidth / 2, CentreY + Ring / 2 + 8);
        return canvas;
    }

    private static void Place(Canvas canvas, UIElement element, double left, double top)
    {
        Canvas.SetLeft(element, left);
        Canvas.SetTop(element, top);
        canvas.Children.Add(element);
    }
}
