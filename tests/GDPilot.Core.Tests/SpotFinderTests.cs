using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class SpotFinderTests
{
    private const int Width = 400;
    private const int Height = 300;

    /// <summary>A dark tree with bright buttons of radius 17 at the given centres, and whatever else is drawn.</summary>
    private static Func<int, int, (byte, byte, byte)> Screen(IEnumerable<ScreenPoint> buttons, Func<int, int, bool>? extra = null)
    {
        List<ScreenPoint> list = buttons.ToList();

        return (x, y) =>
        {
            foreach (ScreenPoint button in list)
            {
                int dx = x - button.X;
                int dy = y - button.Y;

                if (dx * dx + dy * dy <= 17 * 17)
                {
                    return (150, 140, 120);
                }
            }

            if (extra is not null && extra(x, y))
            {
                return (200, 200, 200);
            }

            return (12, 10, 8);
        };
    }

    private static void AssertFound(IEnumerable<ScreenPoint> expected, List<ScreenPoint> found)
    {
        Assert.Equal(expected.Count(), found.Count);

        foreach (ScreenPoint want in expected)
        {
            Assert.Contains(found, spot => Math.Abs(spot.X - want.X) <= 3 && Math.Abs(spot.Y - want.Y) <= 3);
        }
    }

    [Fact]
    public void EveryButtonIsFoundOnceAtItsCentre()
    {
        ScreenPoint[] buttons = { new(60, 60), new(140, 60), new(220, 95), new(60, 200) };

        List<ScreenPoint> found = SpotFinder.Find(Screen(buttons), 0, 0, Width, Height, SpotLook.SkillButton);

        AssertFound(buttons, found);
    }

    [Fact]
    public void AnEmptyTreeHasNoSpots()
    {
        List<ScreenPoint> found = SpotFinder.Find(Screen(Array.Empty<ScreenPoint>()), 0, 0, Width, Height, SpotLook.SkillButton);

        Assert.Empty(found);
    }

    [Fact]
    public void ThinTextAndConnectingLinesAreNotButtons()
    {
        ScreenPoint[] buttons = { new(100, 100), new(260, 100) };

        // A line joining the buttons, and a level label of thin strokes under the first.
        bool Drawn(int x, int y) => (y >= 99 && y <= 101 && x > 100 && x < 260) || (y >= 124 && y <= 134 && x >= 86 && x <= 114 && x % 5 == 0);

        List<ScreenPoint> found = SpotFinder.Find(Screen(buttons, Drawn), 0, 0, Width, Height, SpotLook.SkillButton);

        AssertFound(buttons, found);
    }

    [Fact]
    public void SpotsComeBackInScreenPixelsForAnAreaAwayFromTheCorner()
    {
        ScreenPoint[] buttons = { new(760, 340) };
        Func<int, int, (byte, byte, byte)> screen = Screen(buttons);

        List<ScreenPoint> found = SpotFinder.Find(screen, 700, 300, 200, 120, SpotLook.SkillButton);

        AssertFound(buttons, found);
    }

    [Fact]
    public void GDPilotsOwnCardIsLeftOutOfWhatTheScreenShows()
    {
        ScreenPoint[] buttons = { new(60, 60), new(300, 200) };
        Func<int, int, (byte, byte, byte)> screen = LitPlaces.Without(Screen(buttons), new ScreenRect(30, 30, 60, 60));

        List<ScreenPoint> found = SpotFinder.Find(screen, 0, 0, Width, Height, SpotLook.SkillButton);

        AssertFound(new[] { new ScreenPoint(300, 200) }, found);
    }
}
