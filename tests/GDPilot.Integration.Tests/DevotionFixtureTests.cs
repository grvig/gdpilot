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

        List<ScreenPoint> found = SpotFinder.Find(capture.Pixel, 20, 20, 560, 460, SpotLook.DevotionStar);

        foreach (ScreenPoint star in Crossroads)
        {
            Assert.Contains(found, spot => Math.Abs(spot.X - star.X) <= 6 && Math.Abs(spot.Y - star.Y) <= 6);
        }
    }
}
