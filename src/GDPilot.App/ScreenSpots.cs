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
    /// <summary>Where GDPilot's own card lies over the game right now, which is no part of what the game shows.</summary>
    public Func<ScreenRect?>? Card { get; set; }

    private const int TabMargin = 60;

    public List<ScreenPoint> LitOf(SpotArea area)
    {
        List<ScreenPoint> lit = new();

        foreach (ScreenPoint place in area.Lit)
        {
            int reach = UiFactor.Sized(LitPlaces.Reach, area.Scale);
            int left = place.X - reach;
            int top = place.Y - reach;

            try
            {
                CapturedFrame frame = ScreenCapture.Region(left, top, reach * 2 + 1, reach * 2 + 1);

                if (LitPlaces.Lit((x, y) => Pixel(frame, x - left, y - top), place, area.Scale))
                {
                    lit.Add(place);
                }
            }
            catch (InvalidOperationException)
            {
                // Not being able to see it means not offering it.
            }
        }

        return lit;
    }

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

        Func<int, int, (byte, byte, byte)> pixel = LitPlaces.Without((x, y) => Pixel(frame, x - left, y - top), Card?.Invoke());

        if (area.Kind == SpotKind.Stars && area.Look is not null)
        {
            return StarFinder.Find(pixel, area.Left, area.Top, area.Width, area.Height, area.Look.Spacing, area.Scale);
        }

        if (area.Kind == SpotKind.RiftGates)
        {
            return RiftGateFinder.Find(pixel, area.Left, area.Top, area.Width, area.Height, area.Scale);
        }

        if (area.Kind == SpotKind.SlotList)
        {
            return SlotListReader.Rows(pixel, area.Left, area.Top, area.Width, area.Height, area.Scale);
        }

        if (area.Kind == SpotKind.Lines)
        {
            return RecipeReader.Lines(pixel, area.Left, area.Top, area.Width, area.Height, area.Scale);
        }

        if (area.Kind == SpotKind.Recipes)
        {
            return RecipeReader.Recipes(pixel, area.Left, area.Top, area.Width, area.Height, area.Scale);
        }

        if (area.Kind == SpotKind.Replies || area.Look is null)
        {
            return DialogueReader.Choices(pixel, area.Left, area.Top, area.Width, area.Height, area.Scale);
        }

        return SpotFinder.Find(pixel, area.Left, area.Top, area.Width, area.Height, area.Look);
    }

    public bool Shows(FrameLine line)
    {
        const int Margin = 6;
        int left = line.X - Margin;
        CapturedFrame frame;

        try
        {
            frame = ScreenCapture.Region(left, line.Top, Margin * 2 + 1, line.Bottom - line.Top + 1);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        return line.Seen((x, y) => Pixel(frame, x - left, y - line.Top));
    }

    public int? SelectedTab(GridSpec strip)
    {
        int margin = UiFactor.Sized(TabMargin, Math.Max(1, strip.Scale));
        (int firstX, int firstY) = strip.Centre(new GridCell(0, 0));
        (int lastX, int lastY) = strip.Centre(new GridCell(strip.Columns - 1, strip.Rows - 1));
        int left = firstX - margin;
        int top = Math.Max(0, firstY - margin);
        CapturedFrame frame;

        try
        {
            frame = ScreenCapture.Region(left, top, lastX - firstX + margin * 2, lastY + margin - top);
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        return TabStrip.Selected((x, y) => Pixel(frame, x - left, y - top), strip);
    }

    /// <summary>Whether a conversation's box is on screen. A failed capture says yes, since it proves nothing has closed.</summary>
    public bool ConversationShowing(SpotArea area)
    {
        CapturedFrame frame;

        try
        {
            frame = ScreenCapture.Region(area.Left, area.Top, area.Width, area.Height);
        }
        catch (InvalidOperationException)
        {
            return true;
        }

        return DialogueReader.Showing((x, y) => Pixel(frame, x - area.Left, y - area.Top), area.Left, area.Top, area.Width, area.Height, area.Scale);
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
