using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class GameCommandTests
{
    [Fact]
    public void ALookCommandCarriesBothRatesOnOneLine()
    {
        string line = new LookCommand(1.25, -0.5).Write();

        Assert.Equal("""{"protocol":1,"type":"look","yaw":1.25,"pitch":-0.5}""", line);
        Assert.Equal(new LookCommand(1.25, -0.5), GameCommands.Parse(line));
    }

    [Fact]
    public void TheToggleNeedsNothingButItsType()
    {
        Assert.Equal("""{"protocol":1,"type":"toggle-camera"}""", new ToggleCameraCommand().Write());
        Assert.IsType<ToggleCameraCommand>(GameCommands.Parse(new ToggleCameraCommand().Write()));
    }

    [Fact]
    public void TheGameReportsWhichCameraItShows()
    {
        CameraReport report = Assert.IsType<CameraReport>(GameReport.Parse("""{"protocol":1,"type":"camera","third":true}"""));

        Assert.True(report.ThirdPerson);
        Assert.Equal(report, GameReport.Parse(report.Write()));
    }
}
