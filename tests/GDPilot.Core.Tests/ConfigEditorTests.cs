using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class ConfigEditorTests
{
    private static readonly LiveDisplay Display = new(1920, 1080, 0.5);
    private static readonly GridSpec Bags = new(652, 645, 32, 32, 12, 8);

    [Fact]
    public void AGridSavedIntoAnEmptyConfigurationIsReadyToNavigate()
    {
        string json = ConfigEditor.WithGrid("{}", Display, "Inventory", Bags);

        GridReadiness readiness = GridReadiness.Check(GDPilotConfig.Parse(json), Display);

        Assert.True(readiness.Ready, readiness.Message);
        Assert.Equal(Bags, readiness.Grids["Inventory"]);
    }

    [Fact]
    public void EverythingElseInTheFileSurvives()
    {
        string original = """
            {
              // A comment, which a save does not keep.
              "killSwitch": "Ctrl+Alt+F12",
              "bindings": { "Combat": { "A": "key:Space" } },
              "profiles": { "1920x1080@0.5": { "resolution": [1920, 1080], "uiScale": 0.5, "grids": { "Vendor": { "originX": 483, "originY": 323, "pitchX": 32, "pitchY": 32, "columns": 10, "rows": 18 } } } },
            }
            """;

        GDPilotConfig config = GDPilotConfig.Parse(ConfigEditor.WithGrid(original, Display, "Inventory", Bags));

        Assert.Equal("Ctrl+Alt+F12", config.KillSwitch);
        Assert.Equal("key:Space", config.Bindings["Combat"]["A"]);
        Assert.Equal(10, config.Profiles["1920x1080@0.5"].Grids["Vendor"].Columns);
        Assert.Equal(12, config.Profiles["1920x1080@0.5"].Grids["Inventory"].Columns);
    }

    [Fact]
    public void SavingAgainReplacesTheGrid()
    {
        string once = ConfigEditor.WithGrid("{}", Display, "Inventory", Bags);
        string twice = ConfigEditor.WithGrid(once, Display, "Inventory", Bags with { Rows = 6 });

        Assert.Equal(6, GDPilotConfig.Parse(twice).Profiles["1920x1080@0.5"].Grids["Inventory"].Rows);
    }

    [Fact]
    public void ANewDisplayGetsItsOwnProfileAndBecomesActive()
    {
        string first = ConfigEditor.WithGrid("{}", Display, "Inventory", Bags);
        string second = ConfigEditor.WithGrid(first, new LiveDisplay(2560, 1440, 1.0), "Inventory", Bags);

        GDPilotConfig config = GDPilotConfig.Parse(second);

        Assert.Equal("2560x1440@1.0", config.ActiveProfile);
        Assert.Equal(new[] { 2560, 1440 }, config.Profiles["2560x1440@1.0"].Resolution);
        Assert.True(config.Profiles.ContainsKey("1920x1080@0.5"));
    }

    [Theory]
    [InlineData("Bags")]
    [InlineData("Combat")]
    [InlineData("inventory")]
    public void AGridMustBeNamedAfterAPanelMode(string name)
    {
        Assert.Throws<ArgumentException>(() => ConfigEditor.WithGrid("{}", Display, name, Bags));
    }
}
