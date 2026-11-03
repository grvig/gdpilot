using GDPilot.Core;

namespace GDPilot.Integration.Tests;

/// <summary>
/// The spot finder against real captures of the Soldier's and the Shaman's
/// skill trees at 1920 by 1080, UI scale 0.5, cropped to the skill window,
/// whose top-left corner sat at (460, 225) on screen.
/// </summary>
public sealed class SkillTreeFixtureTests
{
    // Every button in the capture, by eye, in the crop's own pixels.
    private static readonly ScreenPoint[] Soldier =
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

    // The Shaman's tree has a dark unlearned button, and bright artwork beside its first column.
    private static readonly ScreenPoint[] Shaman =
    {
        new(274, 100), new(433, 100), new(513, 100), new(593, 100), new(833, 100),
        new(353, 170), new(513, 170), new(673, 170), new(753, 170), new(913, 170),
        new(433, 202), new(833, 202),
        new(274, 240), new(513, 240), new(753, 240),
        new(353, 272),
        new(433, 310), new(593, 310), new(833, 310),
        new(274, 380), new(353, 380), new(673, 380), new(913, 380),
        new(433, 450), new(593, 450), new(833, 450), new(913, 450),
        new(353, 520), new(513, 520), new(673, 520),
    };

    [Fact]
    public void EveryButtonInTheSoldiersTreeIsFoundAndNothingElse()
    {
        AssertFinds("skills.png", Soldier);
    }

    [Fact]
    public void EveryButtonInTheShamansTreeIsFoundAndNothingElse()
    {
        AssertFinds("shaman.png", Shaman);
    }

    private static void AssertFinds(string file, ScreenPoint[] buttons)
    {
        Frame capture = Frame.Load(Path.Combine(AppContext.BaseDirectory, "screens", file));

        List<ScreenPoint> found = SpotFinder.Find(capture.Pixel, 240, 75, 710, 480, SpotLook.SkillButton);

        Assert.Equal(buttons.Length, found.Count);

        // A button is about 36 pixels across, so a cursor within 15 of its centre is on it.
        foreach (ScreenPoint button in buttons)
        {
            Assert.Contains(found, spot => Math.Abs(spot.X - button.X) <= 15 && Math.Abs(spot.Y - button.Y) <= 15);
        }
    }
}
