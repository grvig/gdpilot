using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class GlideTests
{
    private static readonly TimeSpan Report = TimeSpan.FromMilliseconds(32);
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(4);

    [Fact]
    public void AFirstReadingStaysPut()
    {
        Glide glide = new();
        glide.Report(0.5, 0.4);

        Assert.Equal((0.5, 0.4), glide.Advance(Report));
    }

    [Fact]
    public void BetweenReportsThePointCarriesOnAtItsLastSpeed()
    {
        Glide glide = new();
        glide.Report(0.5, 0.4);
        glide.Advance(Report);
        glide.Report(0.6, 0.4);

        (double x, double _) = glide.Advance(TimeSpan.FromMilliseconds(16));

        Assert.Equal(0.65, x, 6);
    }

    [Fact]
    public void ItNeverRunsAheadByMoreThanOneReportsGap()
    {
        Glide glide = new();
        glide.Report(0.5, 0.4);
        glide.Advance(Report);
        glide.Report(0.6, 0.4);

        (double x, double _) = glide.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(0.7, x, 6);
    }

    [Fact]
    public void StepsAtTheLoopsRateAreSmall()
    {
        Glide glide = new();
        glide.Report(0.5, 0.4);
        glide.Advance(Report);
        glide.Report(0.6, 0.4);
        double before = 0.6;

        for (int tick = 0; tick < 8; tick++)
        {
            (double x, double _) = glide.Advance(Tick);
            Assert.InRange(x - before, 0.0, 0.0126);
            before = x;
        }
    }

    [Fact]
    public void AResetPointInheritsNoSpeed()
    {
        Glide glide = new();
        glide.Report(0.5, 0.4);
        glide.Advance(Report);
        glide.Report(0.6, 0.4);
        glide.Reset();
        glide.Report(0.2, 0.3);

        Assert.Equal((0.2, 0.3), glide.Advance(Report));
    }
}
