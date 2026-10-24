using System.Windows.Media;
using System.Windows.Media.Imaging;
using GDPilot.Core;

namespace GDPilot.Integration.Tests;

/// <summary>
/// Checks the calibration shipped in config.example.json against the real bag
/// capture in tests/fixtures/screens. In empty cells the game draws thin bright
/// grid lines around dark interiors, so wherever the calibration says a cell
/// edge is, the capture must be bright, and wherever it says a centre is, dark.
/// </summary>
public sealed class CalibrationFixtureTests
{
    private readonly GDPilotConfig config = GDPilotConfig.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "config.example.json")));

    [Fact]
    public void TheShippedProfileMatchesTheCapturesDisplay()
    {
        Frame capture = Capture("inventory.png");
        GridReadiness readiness = GridReadiness.Check(config, new LiveDisplay(capture.Width, capture.Height, 0.5));

        Assert.True(readiness.Ready, readiness.Message);
    }

    [Theory]
    [InlineData("inventory.png", "Inventory.Bag2")]
    [InlineData("vendor.png", "Vendor.Bag2")]
    [InlineData("stash.png", "Stash.Bag2")]
    public void SecondBagCellEdgesFallOnTheCapturesGridLines(string file, string gridName)
    {
        Frame capture = Capture(file);
        GridSpec bag = config.Profiles[config.ActiveProfile].Grids[gridName].ToSpec();

        // Columns 5 to 7 of the second bag are empty in the capture.
        for (int column = 5; column < 8; column++)
        {
            for (int row = 0; row < 8; row++)
            {
                (int x, int y) = bag.Centre(new GridCell(column, row));
                double edge = capture.Brightness(x - (int)(bag.PitchX / 2), y - 8, y + 8, true);
                double middle = capture.Brightness(x, y - 8, y + 8, true);

                Assert.True(edge > middle * 1.5, $"Cell ({column}, {row}): edge {edge:F1} is not brighter than middle {middle:F1}.");
            }
        }
    }

    [Theory]
    [InlineData("inventory.png", "Inventory.Bag2")]
    [InlineData("vendor.png", "Vendor.Bag2")]
    [InlineData("stash.png", "Stash.Bag2")]
    public void SecondBagRowEdgesFallOnTheCapturesGridLines(string file, string gridName)
    {
        Frame capture = Capture(file);
        GridSpec bag = config.Profiles[config.ActiveProfile].Grids[gridName].ToSpec();

        for (int row = 0; row < 8; row++)
        {
            (int x, int y) = bag.Centre(new GridCell(7, row));
            double edge = capture.Brightness(y - (int)(bag.PitchY / 2), x - 8, x + 8, false);
            double middle = capture.Brightness(y, x - 8, x + 8, false);

            Assert.True(edge > middle * 1.5, $"Row {row}: edge {edge:F1} is not brighter than middle {middle:F1}.");
        }
    }

    private static Frame Capture(string file)
    {
        return Frame.Load(Path.Combine(AppContext.BaseDirectory, "screens", file));
    }

    private sealed class Frame
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
}
