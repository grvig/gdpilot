using System.Windows.Media;
using System.Windows.Media.Imaging;
using GDPilot.Core;
using GDPilot.Vision;

namespace GDPilot.Integration.Tests;

/// <summary>
/// Grid detection run on the real captures, with the region a person would
/// point at. Expected grids were measured by hand from empty-cell lines and
/// confirmed by hovering items in the game, which shows the right tooltips.
/// </summary>
public sealed class GridDetectionFixtureTests
{
    [Theory]
    [InlineData("inventory.png", 1020, 620, 1295, 895, 1044, 645, 8, 8)]
    [InlineData("inventory.png", 628, 620, 1024, 895, 652, 645, 12, 8)]
    [InlineData("vendor.png", 460, 295, 800, 895, 483, 323, 10, 18)]
    [InlineData("vendor.png", 800, 620, 1196, 895, 824, 645, 12, 8)]
    [InlineData("vendor.png", 1190, 620, 1465, 895, 1217, 645, 8, 8)]
    public void EachGridInTheCapturesIsFound(string capture, int left, int top, int right, int bottom, int originX, int originY, int columns, int rows)
    {
        CapturedFrame frame = Load(capture);

        LineRun? columnLines = GridDetector.FindLines(BrightnessProfile.Columns(frame, left, right, top, bottom), 16, 96);
        LineRun? rowLines = GridDetector.FindLines(BrightnessProfile.Rows(frame, left, right, top, bottom), 16, 96);

        Assert.NotNull(columnLines);
        Assert.NotNull(rowLines);
        GridSpec grid = GridDetector.ToGrid(columnLines.Value, rowLines.Value, left, top);

        // A pixel either way is half a percent of a cell and makes no difference to a click.
        Assert.InRange(grid.OriginX, originX - 1.5, originX + 1.5);
        Assert.InRange(grid.OriginY, originY - 1.5, originY + 1.5);
        Assert.Equal(32, grid.PitchX);
        Assert.Equal(32, grid.PitchY);
        Assert.Equal(columns, grid.Columns);
        Assert.Equal(rows, grid.Rows);
    }

    private static CapturedFrame Load(string capture)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "screens", capture);
        BitmapDecoder decoder = new PngBitmapDecoder(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        BitmapSource image = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgr32, null, 0);
        byte[] pixels = new byte[image.PixelWidth * image.PixelHeight * 4];
        image.CopyPixels(pixels, image.PixelWidth * 4, 0);
        return new CapturedFrame(pixels, image.PixelWidth, image.PixelHeight);
    }
}
