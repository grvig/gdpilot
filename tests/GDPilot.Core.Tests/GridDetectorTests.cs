using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class GridDetectorTests
{
    [Fact]
    public void EvenlySpacedLinesAreFound()
    {
        double[] profile = Profile(300, 10, 32, 9);

        Assert.Equal(new LineRun(10, 32, 9), GridDetector.FindLines(profile, 16, 64));
    }

    [Fact]
    public void ALineHiddenUnderAnItemDoesNotBreakTheRun()
    {
        double[] profile = Profile(300, 10, 32, 9);
        profile[10 + 32 * 4] = 10;

        Assert.Equal(new LineRun(10, 32, 9), GridDetector.FindLines(profile, 16, 64));
    }

    [Fact]
    public void StrayBrightSpotsOffTheGridAreIgnored()
    {
        double[] profile = Profile(300, 10, 32, 9);
        profile[27] = 200;
        profile[155] = 200;

        Assert.Equal(new LineRun(10, 32, 9), GridDetector.FindLines(profile, 16, 64));
    }

    [Fact]
    public void TheTruePitchBeatsAMultipleOfIt()
    {
        // Every other line also forms an even run, at 64. The 32 run is longer and wins.
        Assert.Equal(32, GridDetector.FindLines(Profile(300, 10, 32, 9), 16, 80)!.Value.Spacing);
    }

    [Fact]
    public void FaintHalfCellLinesAreNotMistakenForTheGrid()
    {
        // Strong borders every 32, weaker artefacts halfway between, as in a full bag.
        double[] profile = Profile(300, 10, 32, 9);

        for (int line = 0; line < 8; line++)
        {
            profile[26 + line * 32] = 55;
        }

        Assert.Equal(new LineRun(10, 32, 9), GridDetector.FindLines(profile, 12, 80));
    }

    [Fact]
    public void AGenuinelyFineGridWithEvenLinesIsKept()
    {
        Assert.Equal(new LineRun(10, 16, 17), GridDetector.FindLines(Profile(300, 10, 16, 17), 12, 80));
    }

    [Fact]
    public void AFlatStripHasNoGrid()
    {
        Assert.Null(GridDetector.FindLines(Enumerable.Repeat(20.0, 200).ToArray(), 16, 64));
    }

    [Fact]
    public void ColumnAndRowRunsMakeAGridCentredBetweenLines()
    {
        GridSpec grid = GridDetector.ToGrid(new LineRun(3, 32, 9), new LineRun(9, 32, 9), 1025, 620);

        Assert.Equal(new GridSpec(1044, 645, 32, 32, 8, 8), grid);
    }

    /// <summary>A dark strip with bright one-pixel lines, like a row of empty cells.</summary>
    private static double[] Profile(int length, int first, int spacing, int count)
    {
        double[] profile = Enumerable.Repeat(12.0, length).ToArray();

        for (int line = 0; line < count; line++)
        {
            profile[first + line * spacing] = 90;
        }

        return profile;
    }
}
