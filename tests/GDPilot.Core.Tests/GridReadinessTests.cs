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
}
