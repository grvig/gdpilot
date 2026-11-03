using GDPilot.Core;

namespace GDPilot.Core.Tests;

/// <summary>The grid navigator snapping to spots in a menu that has them, such as the skill tree.</summary>
public sealed class SnappingTests
{
    private static readonly SpotArea Tree = new(700, 300, 710, 480, SpotLook.SkillButton, new[] { new ScreenPoint(645, 847) });

    private static readonly GridReadiness Ready = new(true, "ready", new Dictionary<string, GridSpec>
    {
        ["Inventory"] = new GridSpec(652, 645, 32, 32, 12, 8),
    })
    {
        Spots = new Dictionary<string, SpotArea> { ["Skills"] = Tree },
    };

    private sealed class FakeSpots : ISpotSource
    {
        public List<ScreenPoint>? Showing { get; set; } = new() { new(734, 326), new(896, 320), new(814, 392) };

        public int Reads { get; private set; }

        public List<ScreenPoint>? Read(SpotArea area)
        {
            Reads++;

            if (Showing is null)
            {
                return null;
            }

            return new List<ScreenPoint>(Showing);
        }
    }

    [Fact]
    public void TheFirstPressLandsOnTheTopLeftButton()
    {
        GridNavigator navigator = new(Ready, null, new FakeSpots());

        Assert.Equal(new PointerMoveTo(734, 326), navigator.Act(Mode.Skills, "down"));
    }

    [Fact]
    public void PressesMoveBetweenButtonsByDirection()
    {
        GridNavigator navigator = new(Ready, null, new FakeSpots());
        navigator.Act(Mode.Skills, "down");

        Assert.Equal(new PointerMoveTo(896, 320), navigator.Act(Mode.Skills, "right"));
        Assert.Equal(new PointerMoveTo(814, 392), navigator.Act(Mode.Skills, "down"));
    }

    [Fact]
    public void AFixedPlaceIsReachedLikeAFoundButton()
    {
        GridNavigator navigator = new(Ready, null, new FakeSpots());
        navigator.Act(Mode.Skills, "down");
        navigator.Act(Mode.Skills, "down");

        Assert.Equal(new PointerMoveTo(645, 847), navigator.Act(Mode.Skills, "down"));
    }

    [Fact]
    public void NothingThatWayKeepsTheCursorOnItsButton()
    {
        GridNavigator navigator = new(Ready, null, new FakeSpots());
        navigator.Act(Mode.Skills, "down");

        Assert.Equal(new PointerMoveTo(734, 326), navigator.Act(Mode.Skills, "up"));
    }

    [Fact]
    public void TheScreenIsReadAgainAtEveryPress()
    {
        FakeSpots spots = new();
        GridNavigator navigator = new(Ready, null, spots);
        navigator.Act(Mode.Skills, "down");

        spots.Showing = new List<ScreenPoint> { new(734, 326), new(1100, 330) };

        Assert.Equal(new PointerMoveTo(1100, 330), navigator.Act(Mode.Skills, "right"));
        Assert.Equal(2, spots.Reads);
    }

    [Fact]
    public void AnUnreadableScreenSaysWhyAndMovesNothing()
    {
        FakeSpots spots = new() { Showing = null };
        GridNavigator navigator = new(Ready, null, spots);

        Assert.Null(navigator.Act(Mode.Skills, "down"));
        Assert.NotNull(navigator.Notice);
    }

    [Fact]
    public void GridsStillDriveTheModesWithoutSpots()
    {
        GridNavigator navigator = new(Ready, null, new FakeSpots());

        Assert.Equal(new PointerMoveTo(652, 645), navigator.Act(Mode.Inventory, "right"));
    }
}
