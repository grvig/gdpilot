using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GDPilot.Integration.Tests;

/// <summary>A capture from tests/fixtures/screens, read once into memory.</summary>
internal sealed class Frame
{
    private readonly byte[] pixels;

    private Frame(byte[] pixels, int width, int height)
    {
        this.pixels = pixels;
        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }

    public static Frame Load(string path)
    {
        BitmapDecoder decoder = new PngBitmapDecoder(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        BitmapSource image = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgr32, null, 0);
        byte[] pixels = new byte[image.PixelWidth * image.PixelHeight * 4];
        image.CopyPixels(pixels, image.PixelWidth * 4, 0);
        return new Frame(pixels, image.PixelWidth, image.PixelHeight);
    }

    /// <summary>One pixel as red, green and blue; black outside the capture.</summary>
    public (byte Red, byte Green, byte Blue) Pixel(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return (0, 0, 0);
        }

        int offset = (y * Width + x) * 4;
        return (pixels[offset + 2], pixels[offset + 1], pixels[offset]);
    }

    /// <summary>Mean brightness along a short line: a column at x when vertical, a row at y when not.</summary>
    public double Brightness(int at, int from, int to, bool vertical)
    {
        double sum = 0;

        for (int along = from; along <= to; along++)
        {
            int x = along;
            int y = at;

            if (vertical)
            {
                x = at;
                y = along;
            }

            int offset = (y * Width + x) * 4;
            sum += (pixels[offset] + pixels[offset + 1] + pixels[offset + 2]) / 3.0;
        }

        return sum / (to - from + 1);
    }
}
