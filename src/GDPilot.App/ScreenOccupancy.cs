using GDPilot.Core;
using GDPilot.Vision;

namespace GDPilot.App;

/// <summary>
/// Reads a grid's occupancy off the screen at the moment of a press. Only the
/// grid's own rectangle is captured, a few milliseconds' work, and only while a
/// panel is being navigated, never in combat.
/// </summary>
internal sealed class ScreenOccupancy : IOccupancySource
{
    /// <summary>Room around the outer lines, so the line check beside the edge cells stays inside the capture.</summary>
    private const int Margin = 6;

    public OccupancyMap? Read(string name, GridSpec grid, OccupancyMap? previous)
    {
        int left = (int)Math.Floor(grid.OriginX - grid.PitchX / 2) - Margin;
        int top = (int)Math.Floor(grid.OriginY - grid.PitchY / 2) - Margin;
        int width = (int)Math.Ceiling(grid.PitchX * grid.Columns) + Margin * 2;
        int height = (int)Math.Ceiling(grid.PitchY * grid.Rows) + Margin * 2;
        CapturedFrame frame;

        try
        {
            frame = ScreenCapture.Region(left, top, width, height);
        }
        catch (InvalidOperationException)
        {
            // A failed capture, such as on a locked desktop, means stepping instead.
            return null;
        }

        return OccupancyMap.Read(grid, (x, y) => Pixel(frame, x - left, y - top), previous);
    }

    private static (byte, byte, byte) Pixel(CapturedFrame frame, int x, int y)
    {
        if (x < 0 || y < 0 || x >= frame.Width || y >= frame.Height)
        {
            return (0, 0, 0);
        }

        int offset = (y * frame.Width + x) * 4;
        return (frame.Pixels[offset + 2], frame.Pixels[offset + 1], frame.Pixels[offset]);
    }
}
