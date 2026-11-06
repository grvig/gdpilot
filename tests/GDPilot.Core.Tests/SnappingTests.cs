using GDPilot.Core;

namespace GDPilot.Core.Tests;

/// <summary>The grid navigator snapping to spots in a menu that has them, such as the skill tree.</summary>
public sealed class SnappingTests
{
    private static readonly SpotArea Tree = new(700, 300, 710, 480, SpotLook.SkillButton, new[] { new ScreenPoint(645, 847) });

    private static readonly FrameLine MenuFrame = new(837, 420, 660);
    private static readonly FrameLine OptionsFrame = new(526, 300, 780);

    private static readonly GridReadiness Ready = new(true, "ready", new Dictionary<string, GridSpec>
    {
        ["Inventory"] = new GridSpec(652, 645, 32, 32, 12, 8),
        ["Crafting.Bag1"] = new GridSpec(824, 645, 32, 32, 12, 8),
        ["Skills.Tabs"] = new GridSpec(793, 247, 166, 32, 3, 1),
        ["Vendor"] = new GridSpec(483, 323, 32, 32, 10, 18),
        ["Vendor.Tabs"] = new GridSpec(505, 283, 61.25, 32, 5, 1),
        ["Crafting.Tabs"] = new GridSpec(505, 283, 61.25, 32, 5, 1),
        ["Crafting.Inventor.Tabs"] = new GridSpec(522, 270, 105, 32, 3, 1),
        ["Devotion.Tabs"] = new GridSpec(796, 21, 164, 20, 3, 1),
        ["Stash.Tabs"] = new GridSpec(473, 463, 38, 38, 1, 2),
    })
    {
        Spots = new Dictionary<string, SpotArea>
        {
            ["Skills"] = Tree,
            ["Assign"] = new SpotArea(680, 300, 760, 730, null, Array.Empty<ScreenPoint>(), SpotKind.SlotList),
            ["Crafting"] = new SpotArea(470, 330, 320, 230, null, Array.Empty<ScreenPoint>(), SpotKind.Recipes),
            ["Dialog"] = new SpotArea(773, 300, 477, 635, null, Array.Empty<ScreenPoint>(), SpotKind.Replies),
            ["Inventory.Gear"] = new SpotArea(0, 0, 0, 0, null, new[] { new ScreenPoint(690, 268) }, SpotKind.Places),
            ["Crafting.Gear"] = new SpotArea(0, 0, 0, 0, null, new[] { new ScreenPoint(862, 268) }, SpotKind.Places),
            ["Menu"] = new SpotArea(0, 0, 0, 0, null, new[] { new ScreenPoint(970, 452) }, SpotKind.Places) { When = MenuFrame },
            ["Menu.Options"] = new SpotArea(0, 0, 0, 0, null, new[] { new ScreenPoint(1115, 790) }, SpotKind.Places) { When = OptionsFrame },
        },
    };

    private sealed class FakeSpots : ISpotSource
    {
        public List<ScreenPoint>? Showing { get; set; } = new() { new(734, 326), new(896, 320), new(814, 392) };

        public int Reads { get; private set; }

        public List<ScreenPoint> Lighted { get; set; } = new();

        public List<ScreenPoint> LitOf(SpotArea area)
        {
            return area.Lit.Where(place => Lighted.Contains(place)).ToList();
        }

        public FrameLine? Frame { get; set; }

        public int? Selected { get; set; }

        public bool Shows(FrameLine line)
        {
            return Frame == line;
        }

        public int? SelectedTab(GridSpec strip)
        {
            return Selected;
        }

        public List<ScreenPoint>? Read(SpotArea area)
        {
            Reads++;

            if (area.Kind == SpotKind.Places)
            {
                return new List<ScreenPoint>();
            }

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

        spots.Showing = new List<ScreenPoint> { new(734, 326), new(830, 330) };

        Assert.Equal(new PointerMoveTo(830, 330), navigator.Act(Mode.Skills, "right"));
        Assert.Equal(2, spots.Reads);
    }

    [Fact]
    public void AButtonHiddenByATooltipStaysWithinReach()
    {
        FakeSpots spots = new();
        GridNavigator navigator = new(Ready, null, spots);
        navigator.Act(Mode.Skills, "down");

        // The tooltip of the button under the cursor now covers its right-hand neighbour.
        spots.Showing = new List<ScreenPoint> { new(734, 326), new(814, 392) };

        Assert.Equal(new PointerMoveTo(896, 320), navigator.Act(Mode.Skills, "right"));
    }

    [Fact]
    public void LeavingTheMenuForgetsItsButtons()
    {
        FakeSpots spots = new();
        GridNavigator navigator = new(Ready, null, spots);
        navigator.Act(Mode.Skills, "down");
        navigator.Act(Mode.Inventory, "down");

        spots.Showing = new List<ScreenPoint> { new(1000, 400) };

        Assert.Equal(new PointerMoveTo(1000, 400), navigator.Act(Mode.Skills, "down"));
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

    [Fact]
    public void ANewTabForgetsTheButtonsOfTheOldOne()
    {
        FakeSpots spots = new();
        GridNavigator navigator = new(Ready, null, spots);
        navigator.Act(Mode.Skills, "down");

        navigator.Tab(Mode.Skills, 1);
        spots.Showing = new List<ScreenPoint> { new(734, 326), new(1100, 330) };

        // The next press starts over on the new tab's first button, then moves on among only its own.
        Assert.Equal(new PointerMoveTo(734, 326), navigator.Act(Mode.Skills, "right"));
        Assert.Equal(new PointerMoveTo(1100, 330), navigator.Act(Mode.Skills, "right"));
    }

    [Fact]
    public void EachMenuKeepsItsOwnTab()
    {
        GridNavigator navigator = new(Ready, null, new FakeSpots());
        navigator.Tab(Mode.Stash, 1);

        List<InputIntent> clicks = navigator.Tab(Mode.Skills, 1);

        Assert.Equal(new PointerMoveTo(959, 247), clicks[0]);
    }

    [Fact]
    public void RepliesGoneFromTheConversationAreNotKept()
    {
        FakeSpots spots = new() { Showing = new List<ScreenPoint> { new(833, 860), new(833, 886) } };
        GridNavigator navigator = new(Ready, null, spots);
        navigator.Act(Mode.Dialog, "down");

        // The next step of the conversation has a single reply, higher up.
        spots.Showing = new List<ScreenPoint> { new(833, 820) };

        Assert.Equal(new PointerMoveTo(833, 820), navigator.Act(Mode.Dialog, "up"));
        Assert.Equal(new PointerMoveTo(833, 820), navigator.Act(Mode.Dialog, "down"));
    }

    [Fact]
    public void TheDevotionMapCountsTabsWithTheSkillWindowAndUsesItsOwnStrip()
    {
        GridNavigator navigator = new(Ready, null, new FakeSpots());
        navigator.Tab(Mode.Skills, 1);

        List<InputIntent> clicks = navigator.Tab(Mode.Devotion, 1);

        Assert.Equal(new PointerMoveTo(1124, 21), clicks[0]);
    }

    [Fact]
    public void ACraftersRecipesComeFirstAndNextMovesOnToTheBags()
    {
        FakeSpots spots = new() { Showing = new List<ScreenPoint> { new(520, 346), new(520, 369) } };
        GridNavigator navigator = new(Ready, null, spots);

        Assert.Equal(new PointerMoveTo(520, 346), navigator.Act(Mode.Crafting, "down"));
        Assert.Equal(new PointerMoveTo(520, 369), navigator.Act(Mode.Crafting, "down"));
        Assert.Equal(new PointerMoveTo(824, 645), navigator.Act(Mode.Crafting, "next"));
        Assert.Equal(new PointerMoveTo(824, 677), navigator.Act(Mode.Crafting, "down"));
    }

    [Fact]
    public void AFixedPlacesSurfaceComesAfterTheBags()
    {
        GridNavigator navigator = new(Ready, null, new FakeSpots());

        Assert.Equal(new PointerMoveTo(652, 645), navigator.Act(Mode.Inventory, "down"));
        Assert.Equal(new PointerMoveTo(690, 268), navigator.Act(Mode.Inventory, "next"));
        Assert.Equal(new PointerMoveTo(652, 645), navigator.Act(Mode.Inventory, "next"));
    }

    [Fact]
    public void TheSpotsFollowWhicheverScreenOfAModeIsShowing()
    {
        FakeSpots spots = new() { Frame = MenuFrame };
        GridNavigator navigator = new(Ready, null, spots);

        Assert.Equal(new PointerMoveTo(970, 452), navigator.Act(Mode.Menu, "down"));

        spots.Frame = OptionsFrame;

        Assert.Equal(new PointerMoveTo(1115, 790), navigator.Act(Mode.Menu, "down"));
    }

    [Fact]
    public void AScreenWithNoKnownSpotsMovesNothingAndSaysSo()
    {
        FakeSpots spots = new() { Frame = null };
        GridNavigator navigator = new(Ready, null, spots);

        Assert.Null(navigator.Act(Mode.Menu, "down"));
        Assert.NotNull(navigator.Notice);
    }

    [Fact]
    public void TheNextTabCountsFromWhatTheScreenShows()
    {
        FakeSpots spots = new() { Selected = 2 };
        GridNavigator navigator = new(Ready, null, spots);

        // The game kept the last tab from before; the next one wraps round to the first.
        Assert.Equal(new PointerMoveTo(793, 247), navigator.Tab(Mode.Skills, 1)[0]);
    }

    [Fact]
    public void ATreeChangedByTheCursorDropsTheOldOnesButtons()
    {
        FakeSpots spots = new();
        GridNavigator navigator = new(Ready, null, spots);
        navigator.Act(Mode.Skills, "down");

        // None of the three buttons seen are there now: another tree is showing.
        spots.Showing = new List<ScreenPoint> { new(1200, 500), new(1300, 600), new(1400, 700) };

        Assert.Equal(new PointerMoveTo(1200, 500), navigator.Act(Mode.Skills, "right"));
    }

    [Fact]
    public void ForgettingStartsAMenuOverAtItsFirstButton()
    {
        FakeSpots spots = new();
        GridNavigator navigator = new(Ready, null, spots);
        navigator.Act(Mode.Skills, "down");
        navigator.Act(Mode.Skills, "right");

        navigator.Forget();

        Assert.Equal(new PointerMoveTo(734, 326), navigator.Act(Mode.Skills, "down"));
    }

    [Fact]
    public void AModesRecipesBagsAndGearAreEachASurfaceInTurn()
    {
        FakeSpots spots = new() { Showing = new List<ScreenPoint> { new(520, 346) } };
        GridNavigator navigator = new(Ready, null, spots);

        Assert.Equal(new PointerMoveTo(520, 346), navigator.Act(Mode.Crafting, "down"));
        Assert.Equal(new PointerMoveTo(824, 645), navigator.Act(Mode.Crafting, "next"));
        Assert.Equal(new PointerMoveTo(862, 268), navigator.Act(Mode.Crafting, "next"));
        Assert.Equal(new PointerMoveTo(520, 346), navigator.Act(Mode.Crafting, "next"));
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 3)]
    [InlineData(2, 1)]
    [InlineData(3, 4)]
    public void TheTraderTabOnScreenPicksItsSack(int tab, int sack)
    {
        ReportedOccupancy layouts = new(null);
        layouts.Accept(new LayoutReport($"Vendor#{sack}", 10, 18, new[] { new ItemRect(7, 3, 2, 2, 2) }));
        GridNavigator navigator = new(Ready, layouts, new FakeSpots { Selected = tab });

        Assert.Equal(new PointerMoveTo(579, 387), navigator.Act(Mode.Vendor, "down"));
    }

    [Fact]
    public void TheBuybackTabHasNoSackAndFallsToTheScreen()
    {
        ReportedOccupancy layouts = new(null);
        layouts.Accept(new LayoutReport("Vendor#2", 10, 18, new[] { new ItemRect(7, 3, 2, 2, 2) }));
        GridNavigator navigator = new(Ready, layouts, new FakeSpots { Selected = 4 });

        Assert.Equal(new PointerMoveTo(483, 323), navigator.Act(Mode.Vendor, "down"));
    }

    [Fact]
    public void ASlotsListKeepsItsRowsWhileARowsTooltipHidesItsEdge()
    {
        FakeSpots spots = new() { Showing = new List<ScreenPoint> { new(754, 735), new(754, 769), new(754, 803), new(754, 1007) } };
        GridNavigator navigator = new(Ready, null, spots);

        Assert.Equal(new PointerMoveTo(754, 735), navigator.Act(Mode.Assign, "down"));

        spots.Showing = new List<ScreenPoint>();

        Assert.Equal(new PointerMoveTo(754, 769), navigator.Act(Mode.Assign, "down"));

        // Only the lowest row can be read past this one's tooltip; the rows between are still there.
        spots.Showing = new List<ScreenPoint> { new(754, 1007) };
        Assert.Equal(new PointerMoveTo(754, 803), navigator.Act(Mode.Assign, "down"));
    }

    private static readonly FrameLine SmithFrame = new(478, 630, 745);
    private static readonly FrameLine InventorFrame = new(560, 400, 600);

    private static readonly GridReadiness TwoCrafters = Ready with
    {
        Spots = new Dictionary<string, SpotArea>
        {
            ["Crafting"] = new SpotArea(470, 330, 320, 230, null, new[] { new ScreenPoint(630, 846) }, SpotKind.Recipes) { When = SmithFrame },
            ["Crafting.Inventor"] = new SpotArea(0, 0, 0, 0, null, new[] { new ScreenPoint(627, 500) }, SpotKind.Places) { When = InventorFrame, Tabs = "Crafting.Inventor.Tabs" },
        },
    };

    [Fact]
    public void EachCrafterHasItsOwnStopsAndTabs()
    {
        FakeSpots spots = new() { Frame = InventorFrame, Showing = new List<ScreenPoint>() };
        GridNavigator navigator = new(TwoCrafters, null, spots);

        Assert.Equal(new PointerMoveTo(627, 500), navigator.Act(Mode.Crafting, "down"));
        Assert.Equal(new PointerMoveTo(627, 270), navigator.Tab(Mode.Crafting, 1)[0]);

        navigator.Forget();
        spots.Frame = SmithFrame;

        Assert.Equal(new PointerMoveTo(630, 846), navigator.Act(Mode.Crafting, "down"));
        Assert.Equal(new PointerMoveTo(628, 283), navigator.Tab(Mode.Crafting, 1)[0]);
    }

    [Fact]
    public void AFrameHiddenByATooltipKeepsTheStopsInUse()
    {
        FakeSpots spots = new() { Frame = InventorFrame };
        GridNavigator navigator = new(TwoCrafters, null, spots);
        navigator.Act(Mode.Crafting, "down");

        spots.Frame = null;

        Assert.Equal(new PointerMoveTo(627, 500), navigator.Act(Mode.Crafting, "up"));
    }

    [Fact]
    public void ACrafterNotYetMeasuredSaysSoAndLeavesTheBagsInReach()
    {
        FakeSpots spots = new() { Frame = null };
        GridNavigator navigator = new(TwoCrafters, null, spots);

        Assert.Null(navigator.Act(Mode.Crafting, "down"));
        Assert.NotNull(navigator.Notice);
        Assert.Equal(new PointerMoveTo(824, 645), navigator.Act(Mode.Crafting, "next"));
    }

    private static readonly FrameLine ClassFrame = new(805, 300, 800);

    private static readonly GridReadiness WithClassScreen = Ready with
    {
        Spots = new Dictionary<string, SpotArea>
        {
            ["Skills"] = new SpotArea(700, 300, 710, 480, SpotLook.SkillButton, new[] { new ScreenPoint(645, 847) }) { Lit = new[] { new ScreenPoint(568, 766) } },
            ["Skills.Class"] = new SpotArea(0, 0, 0, 0, null, new[] { new ScreenPoint(637, 325), new ScreenPoint(637, 386) }, SpotKind.Places) { When = ClassFrame },
        },
    };

    [Fact]
    public void TheClassScreenTakesOverFromTheTreeAndGivesWayOnceAClassIsChosen()
    {
        FakeSpots spots = new() { Frame = ClassFrame };
        GridNavigator navigator = new(WithClassScreen, null, spots);

        Assert.Equal(new PointerMoveTo(637, 325), navigator.Act(Mode.Skills, "down"));
        Assert.Equal(new PointerMoveTo(637, 386), navigator.Act(Mode.Skills, "down"));

        spots.Frame = null;

        Assert.Equal(new PointerMoveTo(734, 326), navigator.Act(Mode.Skills, "down"));
    }

    [Fact]
    public void ALitPlaceIsAStopOnlyWhileLit()
    {
        FakeSpots spots = new() { Showing = new List<ScreenPoint> { new(734, 326) } };
        GridNavigator navigator = new(WithClassScreen, null, spots);
        navigator.Act(Mode.Skills, "down");

        Assert.Equal(new PointerMoveTo(645, 847), navigator.Act(Mode.Skills, "down"));
        Assert.Equal(new PointerMoveTo(645, 847), navigator.Act(Mode.Skills, "left"));

        spots.Lighted = new List<ScreenPoint> { new(568, 766) };

        Assert.Equal(new PointerMoveTo(568, 766), navigator.Act(Mode.Skills, "left"));

        spots.Lighted = new List<ScreenPoint>();

        Assert.Equal(new PointerMoveTo(645, 847), navigator.Act(Mode.Skills, "right"));
    }
}
