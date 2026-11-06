using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class GameLayoutTests
{
    private static readonly ScreenRect Game = new(0, 0, 1920, 1080);

    private static ScreenArea Card((int X, int Y) at, int width, int height)
    {
        return new ScreenArea((double)at.X / Game.Width, (double)at.Y / Game.Height, (double)(at.X + width) / Game.Width, (double)(at.Y + height) / Game.Height);
    }

    [Theory]
    [InlineData(Mode.Combat)]
    [InlineData(Mode.Inventory)]
    [InlineData(Mode.Stash)]
    [InlineData(Mode.Vendor)]
    [InlineData(Mode.Skills)]
    [InlineData(Mode.Devotion)]
    [InlineData(Mode.WorldMap)]
    [InlineData(Mode.Menu)]
    public void AModestCardNeverCoversTheGamesOwnInterface(Mode mode)
    {
        (int X, int Y) at = GameLayout.Place(Game, 300, 260, OverlayCorner.TopLeft, mode);

        Assert.DoesNotContain(GameLayout.Reserved(mode), area => Card(at, 300, 260).Overlaps(area));
    }

    [Fact]
    public void AskedForTheMinimapsCornerTheCardMovesToFreeSpace()
    {
        (int X, int Y) at = GameLayout.Place(Game, 300, 260, OverlayCorner.TopRight, Mode.Combat);

        Assert.DoesNotContain(GameLayout.Reserved(Mode.Combat), area => Card(at, 300, 260).Overlaps(area));
        Assert.True(at.X < 960, "The card should have left the right side, where the minimap and quests are.");
    }

    [Fact]
    public void AFreeCornerIsKeptAsAsked()
    {
        Assert.Equal(OverlayPlacement.Place(Game, 300, 260, OverlayCorner.BottomLeft), GameLayout.Place(Game, 300, 260, OverlayCorner.BottomLeft, Mode.Combat));
    }

    [Fact]
    public void WithNowhereFreeTheCardTakesTheCornerCoveringLeast()
    {
        (int X, int Y) at = GameLayout.Place(Game, 1800, 1000, OverlayCorner.TopRight, Mode.Combat);

        Assert.True(at.X >= 0 && at.Y >= 0);
    }

    [Fact]
    public void AreasThatOnlyTouchDoNotOverlap()
    {
        Assert.False(new ScreenArea(0, 0, 0.5, 0.5).Overlaps(new ScreenArea(0.5, 0, 1, 0.5)));
        Assert.True(new ScreenArea(0, 0, 0.5, 0.5).Overlaps(new ScreenArea(0.4, 0.4, 1, 1)));
        Assert.Equal(0.01, new ScreenArea(0, 0, 0.5, 0.5).Shared(new ScreenArea(0.4, 0.4, 1, 1)), 6);
    }

    [Fact]
    public void OnTheDevotionMapOnlyTheAffinitiesAndTheTabsStayClear()
    {
        IReadOnlyList<ScreenArea> reserved = GameLayout.Reserved(Mode.Devotion);

        Assert.Contains(reserved, area => area.Left == 0 && area.Right <= 0.15);
        Assert.Contains(reserved, area => area.Top == 0 && area.Left > 0.3 && area.Right < 0.7);
        Assert.Equal(2, reserved.Count);
    }

    [Fact]
    public void AWindowTakesMoreOfASmallerDisplay()
    {
        ScreenArea measured = GameLayout.Reserved(Mode.Skills)[^1];
        ScreenArea small = GameLayout.Reserved(Mode.Skills, new LiveDisplay(1280, 720, 0.5))[^1];

        Assert.Equal(0.5 + (measured.Left - 0.5) * 1.2, small.Left, 3);
        Assert.Equal(0.5 + (measured.Bottom - 0.5) * 1.2, small.Bottom, 3);
    }

    [Fact]
    public void TheHotbarStaysAtTheFootHoweverLargeItIsDrawn()
    {
        ScreenArea large = GameLayout.Reserved(Mode.Combat, new LiveDisplay(1920, 1080, 1.0))[2];

        Assert.Equal(1, large.Bottom, 3);
        Assert.True(large.Top < GameLayout.Reserved(Mode.Combat)[2].Top);
    }

    [Fact]
    public void TheMinimapStaysInItsCornerOnAnyShapeOfWindow()
    {
        ScreenArea square = GameLayout.Reserved(Mode.Combat, new LiveDisplay(1024, 768, 0.5))[0];

        Assert.Equal(1, square.Right, 3);
        Assert.Equal(0, square.Top, 3);
    }

    [Fact]
    public void ACardStillClearsTheGamesInterfaceAtASmallerDisplay()
    {
        ScreenRect game = new(0, 0, 1280, 720);
        LiveDisplay live = new(1280, 720, 0.5);

        (int x, int y) = GameLayout.Place(game, 200, 170, OverlayCorner.TopRight, Mode.Inventory, live);
        ScreenArea card = new((double)x / 1280, (double)y / 720, (double)(x + 200) / 1280, (double)(y + 170) / 720);

        Assert.DoesNotContain(GameLayout.Reserved(Mode.Inventory, live), area => card.Overlaps(area));
    }
}
