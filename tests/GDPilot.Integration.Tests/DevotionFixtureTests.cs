using GDPilot.Core;

namespace GDPilot.Integration.Tests;

/// <summary>
/// The spot finder against a real capture of the devotion map at 1920 by 1080,
/// UI scale 0.5, cropped from (700, 380) on screen round the Crossroads and the
/// Falcon below it.
/// </summary>
public sealed class DevotionFixtureTests
{
    // The Crossroads' five stars, by eye, in the crop's own pixels.
    private static readonly ScreenPoint[] Crossroads =
    {
        new(258, 54), new(362, 124), new(154, 128), new(196, 246), new(320, 248),
    };

    [Fact]
    public void EveryStarOfTheCrossroadsIsAStop()
    {
        Frame capture = Frame.Load(Path.Combine(AppContext.BaseDirectory, "screens", "devotion.png"));

        List<ScreenPoint> found = StarFinder.Find(capture.Pixel, 20, 20, 560, 460, SpotLook.DevotionStar.Spacing);

        foreach (ScreenPoint star in Crossroads)
        {
            Assert.Contains(found, spot => Math.Abs(spot.X - star.X) <= 8 && Math.Abs(spot.Y - star.Y) <= 8);
        }
    }

    // The same crop, screen coordinates less (700, 380): the Falcon, lit, with its stars and the bright lines of its wings.
    private static readonly ScreenPoint[] FalconStars = { new(285, 329), new(366, 371), new(526, 392) };

    private static readonly ScreenPoint[] FalconLines =
    {
        new(309, 389), new(240, 359), new(260, 343), new(337, 164), new(340, 183), new(400, 318), new(265, 385), new(379, 410), new(351, 441), new(326, 448),
    };

    [Fact]
    public void ALitConstellationsStarsAreStopsAndItsBrightLinesAreNot()
    {
        Frame capture = Frame.Load(Path.Combine(AppContext.BaseDirectory, "screens", "devotion.png"));

        List<ScreenPoint> found = StarFinder.Find(capture.Pixel, 20, 20, 560, 460, SpotLook.DevotionStar.Spacing);

        foreach (ScreenPoint star in FalconStars)
        {
            Assert.Contains(found, spot => Math.Abs(spot.X - star.X) <= 10 && Math.Abs(spot.Y - star.Y) <= 10);
        }

        foreach (ScreenPoint line in FalconLines)
        {
            Assert.DoesNotContain(found, spot => Math.Abs(spot.X - line.X) <= 5 && Math.Abs(spot.Y - line.Y) <= 5);
        }
    }
}
