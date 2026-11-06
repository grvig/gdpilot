using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class GridReadinessTests
{
    private const string Calibrated = """
        {
          "activeProfile": "1920x1080@0.5",
          "profiles": {
            "1920x1080@0.5": {
              "resolution": [1920, 1080],
              "uiScale": 0.5,
              "grids": {
                "inventory": { "originX": 520, "originY": 610, "pitchX": 40, "pitchY": 40, "columns": 12, "rows": 8 },
                "stash": { "originX": 0, "originY": 0, "pitchX": 0, "pitchY": 0, "columns": 0, "rows": 0 }
              }
            }
          }
        }
        """;

    [Fact]
    public void AMatchingDisplayEnablesTheCalibratedGrids()
    {
        GridReadiness readiness = GridReadiness.Check(GDPilotConfig.Parse(Calibrated), new LiveDisplay(1920, 1080, 0.5));

        Assert.True(readiness.Ready);
        Assert.Equal(new[] { "inventory" }, readiness.Grids.Keys);
        Assert.Equal((600, 650), readiness.Grids["inventory"].Centre(new GridCell(2, 1)));
    }

    [Fact]
    public void ADifferentUiScaleMovesTheGridsToWhereTheGameDrawsThem()
    {
        GridReadiness readiness = GridReadiness.Check(GDPilotConfig.Parse(Calibrated), new LiveDisplay(1920, 1080, 1.0));

        Assert.True(readiness.Ready);
        Assert.Equal(40 * 1.415, readiness.Grids["inventory"].PitchX, 3);
    }

    [Fact]
    public void ADifferentResolutionMovesThemToo()
    {
        GridReadiness readiness = GridReadiness.Check(GDPilotConfig.Parse(Calibrated), new LiveDisplay(1280, 720, 0.5));

        Assert.True(readiness.Ready);
        Assert.Equal(40 * 0.8, readiness.Grids["inventory"].PitchX, 3);
        Assert.Equal(640 + (520 - 960) * 0.8, readiness.Grids["inventory"].OriginX, 3);
    }

    [Fact]
    public void AWindowedGameCarriesItsPlacesWithIt()
    {
        GridReadiness readiness = GridReadiness.Check(GDPilotConfig.Parse(Calibrated), new LiveDisplay(1280, 720, 0.5, 320, 180));

        Assert.Equal(320 + 640 + (520 - 960) * 0.8, readiness.Grids["inventory"].OriginX, 3);
    }

    [Fact]
    public void AProfileMadeForThisDisplayComesBeforeTheActiveOne()
    {
        const string two = """
            {
              "activeProfile": "a",
              "profiles": {
                "a": { "resolution": [1920, 1080], "uiScale": 0.5, "grids": { "inventory": { "originX": 500, "originY": 500, "pitchX": 40, "pitchY": 40, "columns": 2, "rows": 2 } } },
                "1280x720@0.5": { "resolution": [1280, 720], "uiScale": 0.5, "grids": { "inventory": { "originX": 111, "originY": 222, "pitchX": 30, "pitchY": 30, "columns": 2, "rows": 2 } } }
              }
            }
            """;

        GridReadiness readiness = GridReadiness.Check(GDPilotConfig.Parse(two), new LiveDisplay(1280, 720, 0.5));

        Assert.Equal(111, readiness.Grids["inventory"].OriginX);
    }

    [Fact]
    public void AProfileThatDoesNotSayItsDisplayIsRefused()
    {
        const string blank = """{ "activeProfile": "a", "profiles": { "a": { "grids": { "inventory": { "originX": 5, "originY": 5, "pitchX": 40, "pitchY": 40, "columns": 2, "rows": 2 } } } } }""";

        Assert.False(GridReadiness.Check(GDPilotConfig.Parse(blank), new LiveDisplay(1920, 1080, 0.5)).Ready);
    }

    [Fact]
    public void AMissingActiveProfileIsRefusedAndNamesTheOneToCalibrate()
    {
        GridReadiness readiness = GridReadiness.Check(GDPilotConfig.Parse("{}"), new LiveDisplay(1920, 1080, 0.5));

        Assert.False(readiness.Ready);
        Assert.Contains("1920x1080@0.5", readiness.Message);
    }

    [Fact]
    public void AProfileWithOnlyPlaceholderGridsIsNotReady()
    {
        string placeholders = Calibrated.Replace("\"columns\": 12", "\"columns\": 0");

        Assert.False(GridReadiness.Check(GDPilotConfig.Parse(placeholders), new LiveDisplay(1920, 1080, 0.5)).Ready);
    }

    [Fact]
    public void TheGamesOwnSettingsFileIsRead()
    {
        string options = "quickBuy                  = true\r\nresolution = 1920 1080\r\nuiScale                   = 0.5\r\nscreenMode = 2\r\n";

        Assert.Equal((1920, 1080, 0.5), GameSettings.Parse(options));
    }

    [Fact]
    public void ASettingsFileMissingAValueGivesNothing()
    {
        Assert.Null(GameSettings.Parse("resolution = 1920 1080\n"));
    }

    [Fact]
    public void SpotAreasWithAKnownLookAreReadyAlongsideTheGrids()
    {
        const string withSpots = """
            {
              "activeProfile": "p",
              "profiles": {
                "p": {
                  "resolution": [1920, 1080],
                  "uiScale": 0.5,
                  "spots": {
                    "Skills": { "left": 700, "top": 300, "width": 710, "height": 480, "look": "skillButton", "fixed": [[645, 847]] },
                    "Devotion": { "left": 0, "top": 0, "width": 1920, "height": 1080, "look": "somethingNew" }
                  }
                }
              }
            }
            """;

        GridReadiness readiness = GridReadiness.Check(GDPilotConfig.Parse(withSpots), new LiveDisplay(1920, 1080, 0.5));

        Assert.True(readiness.Ready, readiness.Message);
        Assert.Equal(new[] { "Skills" }, readiness.Spots.Keys);
        Assert.Equal(SpotLook.SkillButton, readiness.Spots["Skills"].Look);
        Assert.Equal(new[] { new ScreenPoint(645, 847) }, readiness.Spots["Skills"].Fixed);
        Assert.Contains("Snapping ready for Skills", readiness.Message);
    }

    [Fact]
    public void RepliesAndFixedPlacesAreKindsOfSpotsToo()
    {
        const string kinds = """
            {
              "activeProfile": "p",
              "profiles": {
                "p": {
                  "resolution": [1920, 1080],
                  "uiScale": 0.5,
                  "spots": {
                    "Dialog": { "left": 773, "top": 300, "width": 477, "height": 635, "look": "dialogueChoice" },
                    "Devotion": { "left": 240, "top": 110, "width": 1660, "height": 930, "look": "devotionStar" },
                    "Menu": { "look": "places", "fixed": [[970, 452], [970, 512]] },
                    "Crafting": { "left": 470, "top": 330, "width": 320, "height": 230, "look": "recipeLine" },
                    "WorldMap": { "look": "places" }
                  }
                }
              }
            }
            """;

        GridReadiness readiness = GridReadiness.Check(GDPilotConfig.Parse(kinds), new LiveDisplay(1920, 1080, 0.5));

        Assert.Equal(SpotKind.Replies, readiness.Spots["Dialog"].Kind);
        Assert.Equal(SpotKind.Places, readiness.Spots["Menu"].Kind);
        Assert.Equal(SpotKind.Recipes, readiness.Spots["Crafting"].Kind);
        Assert.Equal(SpotLook.DevotionStar, readiness.Spots["Devotion"].Look);
        Assert.Equal(2, readiness.Spots["Menu"].Fixed.Count);
        Assert.False(readiness.Spots.ContainsKey("WorldMap"));
    }

    [Fact]
    public void SpotsMoveWithTheEdgeTheyAreAnchoredToAndCarryTheirScale()
    {
        const string withSpots = """
            {
              "activeProfile": "p",
              "profiles": {
                "p": {
                  "resolution": [1920, 1080],
                  "uiScale": 0.5,
                  "grids": { "Hotbar": { "originX": 717, "originY": 1059, "pitchX": 44, "pitchY": 1, "columns": 12, "rows": 1 } },
                  "spots": {
                    "Skills": { "left": 700, "top": 300, "width": 710, "height": 480, "look": "skillButton", "lit": [[568, 766]], "fixed": [[645, 847]], "when": [805, 300, 800] },
                    "Journal": { "left": 20, "top": 545, "width": 320, "height": 350, "look": "textLine", "fixed": [[43, 195]] }
                  }
                }
              }
            }
            """;

        GridReadiness readiness = GridReadiness.Check(GDPilotConfig.Parse(withSpots), new LiveDisplay(1280, 720, 0.5));
        SpotArea skills = readiness.Spots["Skills"];

        Assert.Equal(0.8, skills.Scale, 3);
        Assert.Equal(568, skills.Width);
        Assert.Equal(640 + (700 - 960) * 0.8, skills.Left, 1);
        Assert.Equal(new ScreenPoint(388, 606), skills.Fixed[0]);
        Assert.Equal(new ScreenPoint(326, 541), skills.Lit[0]);
        Assert.Equal(new FrameLine(516, 168, 568, skills.Scale), skills.When);
        Assert.Equal(10, skills.Look!.DiscRadius);
        Assert.Equal(new ScreenPoint(34, 84), readiness.Spots["Journal"].Fixed[0]);
        Assert.Equal(720 - (1080 - 1059) * 0.8, readiness.Grids["Hotbar"].OriginY, 3);
    }

    private const string WithTheDevotionMap = """
        {
          "activeProfile": "p",
          "profiles": {
            "p": {
              "resolution": [1920, 1080],
              "uiScale": 0.5,
              "grids": { "Devotion.Tabs": { "originX": 796, "originY": 21, "pitchX": 164, "pitchY": 20, "columns": 3, "rows": 1 } },
              "spots": {
                "Devotion": { "left": 240, "top": 110, "width": 1660, "height": 960, "look": "devotionStar", "fixed": [[648, 21], [891, 64], [230, 540]] },
                "Dialog": { "left": 773, "top": 300, "width": 477, "height": 635, "look": "dialogueChoice" }
              }
            }
          }
        }
        """;

    [Fact]
    public void TheDevotionMapsStarsKeepTheirSizeAndAreLookedForAcrossTheWholeWindow()
    {
        GridReadiness readiness = GridReadiness.Check(GDPilotConfig.Parse(WithTheDevotionMap), new LiveDisplay(1024, 768, 0.5, 448, 156));
        SpotArea map = readiness.Spots["Devotion"];

        Assert.Equal(1, map.Scale);
        Assert.Equal(SpotLook.DevotionStar, map.Look);
        Assert.Equal(448 + 188, map.Left);
        Assert.Equal(156 + 86, map.Top);
        Assert.Equal(448 + 1024 - 16, map.Left + map.Width);
        Assert.Equal(156 + 768 - 8, map.Top + map.Height);
    }

    [Fact]
    public void WhatLiesOverTheDevotionMapKeepsToItsOwnEdges()
    {
        GridReadiness readiness = GridReadiness.Check(GDPilotConfig.Parse(WithTheDevotionMap), new LiveDisplay(1024, 768, 0.5));
        SpotArea map = readiness.Spots["Devotion"];

        Assert.Equal(new ScreenPoint(267, 16), map.Fixed[0]);
        Assert.Equal(new ScreenPoint(181, 384), map.Fixed[2]);
        Assert.Equal(16, readiness.Grids["Devotion.Tabs"].OriginY, 0);
    }
}
