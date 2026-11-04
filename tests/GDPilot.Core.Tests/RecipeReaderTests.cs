using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class RecipeReaderTests
{
    /// <summary>A dark list with a white heading and two cyan recipes under it.</summary>
    private static (byte, byte, byte) List(int x, int y)
    {
        bool text = x >= 20 && x < 160 && x % 3 == 0;

        if (text && y >= 10 && y < 24)
        {
            return (190, 190, 170);
        }

        if (text && ((y >= 40 && y < 55) || (y >= 63 && y < 78)))
        {
            return (0, 230, 230);
        }

        return (12, 12, 12);
    }

    [Fact]
    public void EachCyanLineIsARecipeAndTheHeadingIsNot()
    {
        List<ScreenPoint> recipes = RecipeReader.Recipes(List, 0, 0, 300, 120);

        Assert.Equal(new[] { new ScreenPoint(50, 47), new ScreenPoint(50, 70) }, recipes);
    }
}
