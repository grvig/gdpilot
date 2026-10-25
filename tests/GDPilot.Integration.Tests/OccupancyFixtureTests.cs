using System.Windows.Media;
using System.Windows.Media.Imaging;
using GDPilot.Core;
using GDPilot.Vision;

namespace GDPilot.Integration.Tests;

/// <summary>
/// The occupancy map read from the real captures, cell by cell, against labels
/// in tests/fixtures/screens/occupancy.txt that were checked by eye. The grids
/// are the ones shipped in config.example.json.
/// </summary>
public sealed class OccupancyFixtureTests
{
    private static readonly string Screens = Path.Combine(AppContext.BaseDirectory, "screens");

    public static IEnumerable<object[]> Blocks()
    {
        string[] lines = File.ReadAllLines(Path.Combine(Screens, "occupancy.txt"));

        for (int index = 0; index < lines.Length; index++)
        {
            string[] heading = lines[index].Split(' ');

            if (heading.Length == 2 && heading[0].EndsWith(".png", StringComparison.Ordinal))
            {
                yield return new object[] { heading[0], heading[1] };
            }
        }
    }

    [Theory]
    [MemberData(nameof(Blocks))]
    public void EveryCellReadsAsLabelled(string capture, string gridName)
    {
        GDPilotConfig config = GDPilotConfig.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "config.example.json")));
        GridSpec grid = config.Profiles[config.ActiveProfile].Grids[gridName].ToSpec();
        CapturedFrame frame = Load(capture);
        List<string> expected = Labels(capture, gridName);

        OccupancyMap map = OccupancyMap.Read(grid, (x, y) => Pixel(frame, x, y));

        Assert.True(map.IsConfident);

        for (int row = 0; row < grid.Rows; row++)
        {
            string read = "";

            for (int column = 0; column < grid.Columns; column++)
            {
                read += Symbol(map[new GridCell(column, row)]);
            }

            Assert.Equal(expected[row], read);
        }
    }

    private static string Symbol(CellState state)
    {
        switch (state)
        {
            case CellState.Occupied:
                return "#";
            case CellState.Empty:
                return ".";
            default:
                return "?";
        }
    }

    private static List<string> Labels(string capture, string gridName)
    {
        List<string> lines = File.ReadAllLines(Path.Combine(Screens, "occupancy.txt")).ToList();
        int start = lines.IndexOf($"{capture} {gridName}") + 1;
        return lines.Skip(start).TakeWhile(line => line.Length > 0).ToList();
    }

    private static (byte, byte, byte) Pixel(CapturedFrame frame, int x, int y)
    {
        int offset = (y * frame.Width + x) * 4;
        return (frame.Pixels[offset + 2], frame.Pixels[offset + 1], frame.Pixels[offset]);
    }

    private static CapturedFrame Load(string capture)
    {
        BitmapDecoder decoder = new PngBitmapDecoder(new Uri(Path.Combine(Screens, capture)), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        BitmapSource image = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgr32, null, 0);
        byte[] pixels = new byte[image.PixelWidth * image.PixelHeight * 4];
        image.CopyPixels(pixels, image.PixelWidth * 4, 0);
        return new CapturedFrame(pixels, image.PixelWidth, image.PixelHeight);
    }
}
