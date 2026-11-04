using GDPilot.Core;

namespace GDPilot.Integration.Tests;

/// <summary>
/// The recipe reader against a real capture of Duncan's window in Devil's
/// Crossing at 1920 by 1080, UI scale 0.5, cropped from (460, 180) on screen,
/// with six empowered relics listed under their heading.
/// </summary>
public sealed class BlacksmithFixtureTests
{
    [Fact]
    public void EachOfTheSixRecipesIsFoundOnItsLine()
    {
        Frame capture = Frame.Load(Path.Combine(AppContext.BaseDirectory, "screens", "blacksmith.png"));

        List<ScreenPoint> recipes = RecipeReader.Recipes(capture.Pixel, 10, 125, 330, 255);

        Assert.Equal(6, recipes.Count);

        for (int index = 0; index < recipes.Count; index++)
        {
            Assert.InRange(recipes[index].Y, 163 + index * 23, 169 + index * 23);
        }
    }
}
