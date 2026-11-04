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
    public void ADifferentUiScaleIsRefusedWithAReason()
    {
        GridReadiness readiness = GridReadiness.Check(GDPilotConfig.Parse(Calibrated), new LiveDisplay(1920, 1080, 1.0));

        Assert.False(readiness.Ready);
        Assert.Empty(readiness.Grids);
        Assert.Contains("Recalibrate", readiness.Message);
    }

    [Fact]
    public void ADifferentResolutionIsRefused()
    {
        Assert.False(GridReadiness.Check(GDPilotConfig.Parse(Calibrated), new LiveDisplay(2560, 1440, 0.5)).Ready);
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
                    "WorldMap": { "look": "places" }
                  }
                }
              }
            }
            """;

        GridReadiness readiness = GridReadiness.Check(GDPilotConfig.Parse(kinds), new LiveDisplay(1920, 1080, 0.5));

        Assert.Equal(SpotKind.Replies, readiness.Spots["Dialog"].Kind);
        Assert.Equal(SpotKind.Places, readiness.Spots["Menu"].Kind);
        Assert.Equal(SpotLook.DevotionStar, readiness.Spots["Devotion"].Look);
        Assert.Equal(2, readiness.Spots["Menu"].Fixed.Count);
        Assert.False(readiness.Spots.ContainsKey("WorldMap"));
    }
}
