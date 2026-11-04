using GDPilot.Core;
using GDPilot.Vision;

namespace GDPilot.App;

/// <summary>
/// Finds a menu's spots on screen at the moment of a press. Only the area and
/// the ring round its edge are captured, and only while a menu is being
/// navigated, never in combat.
/// </summary>
internal sealed class ScreenSpots : ISpotSource
{
    public List<ScreenPoint>? Read(SpotArea area)
    {
        if (area.Kind == SpotKind.Places)
        {
            return new List<ScreenPoint>();
        }

        int margin = 0;

        if (area.Look is not null)
        {
            margin = area.Look.RingRadius + 1;
        }

        int left = area.Left - margin;
        int top = area.Top - margin;
        CapturedFrame frame;

        try
        {
            frame = ScreenCapture.Region(left, top, area.Width + margin * 2, area.Height + margin * 2);
        }
        catch (InvalidOperationException)
        {
            // A failed capture, such as on a locked desktop, means no snapping this press.
            return null;
        }

        Func<int, int, (byte, byte, byte)> pixel = (x, y) => Pixel(frame, x - left, y - top);

        if (area.Kind == SpotKind.Recipes)
        {
            return RecipeReader.Recipes(pixel, area.Left, area.Top, area.Width, area.Height);
        }

        if (area.Kind == SpotKind.Replies || area.Look is null)
        {
            return DialogueReader.Choices(pixel, area.Left, area.Top, area.Width, area.Height);
        }

        return SpotFinder.Find(pixel, area.Left, area.Top, area.Width, area.Height, area.Look);
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
