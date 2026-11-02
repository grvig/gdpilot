using GDPilot.Core;

namespace GDPilot.Integration.Tests;

/// <summary>
/// The spot finder against a real capture of the Soldier's skill tree at 1920
/// by 1080, UI scale 0.5, cropped to the skill window, whose top-left corner
/// sat at (460, 225) on screen.
/// </summary>
public sealed class SkillTreeFixtureTests
{
    // Every button in the capture, by eye, in the crop's own pixels.
    private static readonly ScreenPoint[] Buttons =
    {
        new(274, 100), new(433, 100), new(593, 100), new(833, 100),
        new(513, 132), new(673, 132),
        new(353, 170), new(433, 170), new(753, 170), new(913, 170),
        new(274, 240), new(513, 240), new(673, 240),
        new(353, 272),
        new(433, 310), new(753, 310), new(833, 310),
        new(353, 342),
        new(274, 380), new(513, 380), new(833, 380),
        new(353, 450), new(433, 450), new(513, 450), new(593, 450), new(753, 450), new(913, 450),
        new(353, 520), new(433, 520), new(513, 520), new(673, 520), new(833, 520),
    };

    [Fact]
    public void EveryButtonInTheTreeIsFoundAndNothingElse()
    {
        Frame capture = Frame.Load(Path.Combine(AppContext.BaseDirectory, "screens", "skills.png"));

        List<ScreenPoint> found = SpotFinder.Find(capture.Pixel, 240, 75, 710, 480, SpotLook.SkillButton);

        Assert.Equal(Buttons.Length, found.Count);

        // A button is about 36 pixels across, so a cursor within 15 of its centre is on it.
        foreach (ScreenPoint button in Buttons)
        {
            Assert.Contains(found, spot => Math.Abs(spot.X - button.X) <= 15 && Math.Abs(spot.Y - button.Y) <= 15);
        }
    }
}
