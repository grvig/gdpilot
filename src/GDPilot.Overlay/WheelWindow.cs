using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
    private IReadOnlyList<Picture?>? shownPictures;

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
        if (wheel.Labels.Count != shownSlots || !ReferenceEquals(wheel.Pictures, shownPictures))
        {
            Build(wheel.Labels, wheel.Pictures);
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

    /// <summary>The skill's icon with its key small in the corner, or the key alone when there is no icon.</summary>
    private static UIElement Face(TextBlock label, IReadOnlyList<Picture?>? pictures, int slot)
    {
        if (pictures is null || slot >= pictures.Count || pictures[slot] is not Picture picture)
        {
            return label;
        }

        BitmapSource icon = BitmapSource.Create(picture.Width, picture.Height, 96, 96, PixelFormats.Bgr32, null, picture.Bgra, picture.Width * 4);
        label.FontSize = 14;
        label.HorizontalAlignment = HorizontalAlignment.Right;
        label.VerticalAlignment = VerticalAlignment.Bottom;
        Grid face = new();
        face.Children.Add(new Image { Source = icon, Stretch = Stretch.Uniform });
        face.Children.Add(label);
        return face;
    }

    private void Build(IReadOnlyList<string> labels, IReadOnlyList<Picture?>? pictures)
    {
        canvas.Children.Clear();
        discs.Clear();
        shownSlots = labels.Count;
        shownPictures = pictures;

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
                Padding = new Thickness(6),
                Child = Face(text, pictures, slot),
            };
            Canvas.SetLeft(disc, Size / 2 + Ring * Math.Sin(angle) - Disc / 2);
            Canvas.SetTop(disc, Size / 2 - Ring * Math.Cos(angle) - Disc / 2);
            canvas.Children.Add(disc);
            discs.Add(disc);
        }
    }
}
