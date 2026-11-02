using GDPilot.Core;

namespace GDPilot.Integration.Tests;

/// <summary>
/// Checks the calibration shipped in config.example.json against the real bag
/// capture in tests/fixtures/screens. In empty cells the game draws thin bright
/// grid lines around dark interiors, so wherever the calibration says a cell
/// edge is, the capture must be bright, and wherever it says a centre is, dark.
/// </summary>
public sealed class CalibrationFixtureTests
{
    private readonly GDPilotConfig config = GDPilotConfig.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "config.example.json")));

    [Fact]
    public void TheShippedProfileMatchesTheCapturesDisplay()
    {
        Frame capture = Capture("inventory.png");
        GridReadiness readiness = GridReadiness.Check(config, new LiveDisplay(capture.Width, capture.Height, 0.5));

        Assert.True(readiness.Ready, readiness.Message);
    }

    [Theory]
    [InlineData("inventory.png", "Inventory.Bag2")]
    [InlineData("vendor.png", "Vendor.Bag2")]
    [InlineData("stash.png", "Stash.Bag2")]
    public void SecondBagCellEdgesFallOnTheCapturesGridLines(string file, string gridName)
    {
        Frame capture = Capture(file);
        GridSpec bag = config.Profiles[config.ActiveProfile].Grids[gridName].ToSpec();

        // Columns 5 to 7 of the second bag are empty in the capture.
        for (int column = 5; column < 8; column++)
        {
            for (int row = 0; row < 8; row++)
            {
                (int x, int y) = bag.Centre(new GridCell(column, row));
                double edge = capture.Brightness(x - (int)(bag.PitchX / 2), y - 8, y + 8, true);
                double middle = capture.Brightness(x, y - 8, y + 8, true);

                Assert.True(edge > middle * 1.5, $"Cell ({column}, {row}): edge {edge:F1} is not brighter than middle {middle:F1}.");
            }
        }
    }

    [Theory]
    [InlineData("inventory.png", "Inventory.Bag2")]
    [InlineData("vendor.png", "Vendor.Bag2")]
    [InlineData("stash.png", "Stash.Bag2")]
    public void SecondBagRowEdgesFallOnTheCapturesGridLines(string file, string gridName)
    {
        Frame capture = Capture(file);
        GridSpec bag = config.Profiles[config.ActiveProfile].Grids[gridName].ToSpec();

        for (int row = 0; row < 8; row++)
        {
            (int x, int y) = bag.Centre(new GridCell(7, row));
            double edge = capture.Brightness(y - (int)(bag.PitchY / 2), x - 8, x + 8, false);
            double middle = capture.Brightness(y, x - 8, x + 8, false);

            Assert.True(edge > middle * 1.5, $"Row {row}: edge {edge:F1} is not brighter than middle {middle:F1}.");
        }
    }

    private static Frame Capture(string file)
    {
        return Frame.Load(Path.Combine(AppContext.BaseDirectory, "screens", file));
    }
}
