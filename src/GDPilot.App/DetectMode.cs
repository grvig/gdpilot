using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GDPilot.Core;
using GDPilot.Vision;

namespace GDPilot.App;

/// <summary>
/// Finds a grid in a region of a frame. Used by the calibration wizard on a live
/// capture, and on its own to measure a saved capture: --detect file left top right bottom.
/// </summary>
internal static class DetectMode
{
    /// <summary>Grim Dawn's cells at any sensible UI scale are between these sizes, in pixels.</summary>
    private const int SmallestCell = 16;
    private const int LargestCell = 96;

    internal static int Run(string[] values)
    {
        int[] box = values.Skip(1).Take(4).Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToArray();
        CapturedFrame frame = Load(values[0]);

        // The right and bottom edges are the last pixels read, so they must lie inside the image.
        if (box[0] < 0 || box[1] < 0 || box[2] >= frame.Width || box[3] >= frame.Height || box[0] >= box[2] || box[1] >= box[3])
        {
            Console.Error.WriteLine($"The rectangle must lie inside the {frame.Width} by {frame.Height} image, with left below right and top below bottom.");
            return 2;
        }

        GridSpec? grid = Find(frame, box[0], box[1], box[2], box[3]);

        if (grid is null)
        {
            Console.Error.WriteLine("No evenly spaced grid lines found in that region.");
            return 1;
        }

        Console.WriteLine(Describe(grid));
        return 0;
    }

    internal static GridSpec? Find(CapturedFrame frame, int left, int top, int right, int bottom)
    {
        LineRun? columns = GridDetector.FindLines(BrightnessProfile.Columns(frame, left, right, top, bottom), SmallestCell, LargestCell);
        LineRun? rows = GridDetector.FindLines(BrightnessProfile.Rows(frame, left, right, top, bottom), SmallestCell, LargestCell);

        if (columns is null || rows is null)
        {
            return null;
        }

        return GridDetector.ToGrid(columns.Value, rows.Value, left, top);
    }

    internal static string Describe(GridSpec grid)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{{ \"originX\": {grid.OriginX:0.#}, \"originY\": {grid.OriginY:0.#}, \"pitchX\": {grid.PitchX:0.#}, \"pitchY\": {grid.PitchY:0.#}, \"columns\": {grid.Columns}, \"rows\": {grid.Rows} }}");
    }

    internal static CapturedFrame Load(string path)
    {
        BitmapDecoder decoder = new PngBitmapDecoder(new Uri(Path.GetFullPath(path)), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        BitmapSource image = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgr32, null, 0);
        byte[] pixels = new byte[image.PixelWidth * image.PixelHeight * 4];
        image.CopyPixels(pixels, image.PixelWidth * 4, 0);
        return new CapturedFrame(pixels, image.PixelWidth, image.PixelHeight);
    }
}
