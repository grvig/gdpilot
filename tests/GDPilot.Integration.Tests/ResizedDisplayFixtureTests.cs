using GDPilot.Core;

namespace GDPilot.Integration.Tests;

/// <summary>
/// The shipped profile, measured at 1920 by 1080 at UI scale 0.5, moved to other
/// displays and read against real captures of them: the smallest window the game
/// draws at full size and the largest interface it offers. Each crop keeps its
/// place on the capture, so a wrong place fails as surely as a wrong reading.
/// </summary>
public sealed class ResizedDisplayFixtureTests
{
    private static readonly GDPilotConfig Config = GDPilotConfig.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "config.example.json")));

    private static GridReadiness At(int width, int height, double uiScale)
    {
        return GridReadiness.Check(Config, new LiveDisplay(width, height, uiScale));
    }

    /// <summary>A crop read as the screen it was cut from.</summary>
    private static Func<int, int, (byte Red, byte Green, byte Blue)> Screen(string file, int left, int top)
    {
        Frame crop = Frame.Load(Path.Combine(AppContext.BaseDirectory, "screens", file + ".png"));
        return (x, y) => crop.Pixel(x - left, y - top);
    }

    [Theory]
    [InlineData(1024, 768, 0.5, "sized-1024-skills", 280, 170)]
    [InlineData(1920, 1080, 1.0, "sized-1415-skills", 560, 170)]
    public void EverySkillButtonIsFound(int width, int height, double uiScale, string file, int left, int top)
    {
        SpotArea area = At(width, height, uiScale).Spots["Skills"];

        List<ScreenPoint> found = SpotFinder.Find(Screen(file, left, top), area.Left, area.Top, area.Width, area.Height, area.Look!);

        Assert.Equal(32, found.Count);
    }

    [Theory]
    [InlineData(1024, 768, 0.5, "sized-1024-crafter", 100, 200, 232, 18)]
    [InlineData(1920, 1080, 1.0, "sized-1415-crafter", 240, 220, 263, 32)]
    public void EveryRecipeIsARowApart(int width, int height, double uiScale, string file, int left, int top, int firstY, int apart)
    {
        SpotArea area = At(width, height, uiScale).Spots["Crafting"];

        List<ScreenPoint> found = RecipeReader.Recipes(Screen(file, left, top), area.Left, area.Top, area.Width, area.Height, area.Scale);

        Assert.Equal(6, found.Count);
        Assert.Equal(firstY, found[0].Y);
        Assert.InRange(found[5].Y - found[0].Y, apart * 5 - 2, apart * 5 + 2);
    }

    [Theory]
    [InlineData(1024, 768, 0.5, "sized-1024-gate", 525, 341, 575, 391)]
    [InlineData(1920, 1080, 1.0, "sized-1415-gate", 1014, 495, 1074, 555)]
    public void TheRiftGateIsFoundOnTheMap(int width, int height, double uiScale, string file, int left, int top, int gateX, int gateY)
    {
        double scale = At(width, height, uiScale).Spots["WorldMap"].Scale;

        List<ScreenPoint> found = RiftGateFinder.Find(Screen(file, left, top), left, top, 100, 100, scale);

        ScreenPoint gate = Assert.Single(found);
        Assert.InRange(gate.X, gateX - 2, gateX + 2);
        Assert.InRange(gate.Y, gateY - 2, gateY + 2);
    }

    [Theory]
    [InlineData(1024, 768, 0.5, "sized-1024-journal", 0, 380)]
    [InlineData(1920, 1080, 1.0, "sized-1415-journal", 0, 530)]
    public void EveryLineOfTheJournalIsFound(int width, int height, double uiScale, string file, int left, int top)
    {
        SpotArea area = At(width, height, uiScale).Spots["Journal"];

        List<ScreenPoint> found = RecipeReader.Lines(Screen(file, left, top), area.Left, area.Top, area.Width, area.Height, area.Scale);

        Assert.Equal(8, found.Count);
    }

    [Theory]
    [InlineData(1024, 768, 0.5, "sized-1024-crafter", 100, 200)]
    [InlineData(1600, 900, 0.5, "sized-900-crafter", 360, 250)]
    [InlineData(1920, 1080, 1.0, "sized-1415-crafter", 240, 220)]
    public void TheCraftersFrameShowsThoughItsLineIsSmeared(int width, int height, double uiScale, string file, int left, int top)
    {
        FrameLine line = At(width, height, uiScale).Spots["Crafting"].When!.Value;

        Assert.True(line.Seen(Screen(file, left, top)));
    }

    [Theory]
    [InlineData(1024, 768, 0.5, "sized-1024-tabs", 80, 80)]
    [InlineData(1920, 1080, 1.0, "sized-1415-tabs", 150, 120)]
    public void TheTradersSelectedTabIsRead(int width, int height, double uiScale, string file, int left, int top)
    {
        GridSpec strip = At(width, height, uiScale).Grids["Vendor.Tabs"];

        Assert.Equal(0, TabStrip.Selected(Screen(file, left, top), strip));
    }

    [Theory]
    [InlineData(1920, 1080, 0.5, "sized-1000-characters", 0, 540)]
    [InlineData(1024, 768, 0.5, "sized-1024-characters", 0, 340)]
    [InlineData(1920, 1080, 1.0, "sized-1415-characters", 0, 330)]
    public void EachCharacterOnTheMainMenuIsOneStopThoughTheChosenOneLightsTwoLines(int width, int height, double uiScale, string file, int left, int top)
    {
        SpotArea area = At(width, height, uiScale).Spots["MainMenu"];

        List<ScreenPoint> found = RecipeReader.Lines(Screen(file, left, top), area.Left, area.Top, area.Width, area.Height, area.Scale, area.Apart);

        Assert.Equal(3, found.Count);
    }

    [Theory]
    [InlineData(1280, 720, 0.5, "sized-720-slotlist", 540, 400, 629, 661)]
    [InlineData(1600, 900, 0.5, "sized-900-slotlist", 700, 560, 781, 837)]
    [InlineData(1920, 1080, 1.0, "sized-1415-slotlist", 800, 540, 915, 977)]
    public void EveryRowOfASlotsListIsFoundAndNotTheGroundAboveIt(int width, int height, double uiScale, string file, int left, int top, int rowX, int lowestRow)
    {
        SpotArea area = At(width, height, uiScale).Spots["Assign"];

        List<ScreenPoint> found = SlotListReader.Rows(Screen(file, left, top), area.Left, area.Top, area.Width, area.Height, area.Scale);

        Assert.Equal(9, found.Count);
        Assert.Equal(rowX, found[8].X);
        Assert.Equal(lowestRow, found[8].Y);
    }
}
