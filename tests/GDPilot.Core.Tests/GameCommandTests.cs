using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class GameCommandTests
{
    [Fact]
    public void ALookCommandCarriesBothRatesOnOneLine()
    {
        string line = new LookCommand(1.25, -0.5).Write();

        Assert.Equal("""{"protocol":1,"type":"look","yaw":1.25,"zoom":-0.5}""", line);
        Assert.Equal(new LookCommand(1.25, -0.5), GameCommands.Parse(line));
    }

    [Fact]
    public void TheToggleNeedsNothingButItsType()
    {
        Assert.Equal("""{"protocol":1,"type":"toggle-camera"}""", new ToggleCameraCommand().Write());
        Assert.IsType<ToggleCameraCommand>(GameCommands.Parse(new ToggleCameraCommand().Write()));
    }

    [Fact]
    public void ALockNamesTheTargetTheGameReported()
    {
        string line = new LockCommand(34539).Write();

        Assert.Equal("""{"protocol":1,"type":"lock","id":34539}""", line);
        Assert.Equal(new LockCommand(34539), GameCommands.Parse(line));
    }

    [Fact]
    public void ALockOfZeroLetsTheCameraGo()
    {
        Assert.Equal(new LockCommand(0), GameCommands.Parse("""{"protocol":1,"type":"lock","id":0}"""));
    }

    [Fact]
    public void RecenteringNeedsNothingButItsType()
    {
        Assert.Equal("""{"protocol":1,"type":"recenter"}""", new RecenterCommand().Write());
        Assert.IsType<RecenterCommand>(GameCommands.Parse(new RecenterCommand().Write()));
    }

    [Fact]
    public void AMoveCarriesTheStickRounded()
    {
        string line = new MoveCommand(0.70712, -0.5).Write();

        Assert.Equal("""{"protocol":1,"type":"move","x":0.707,"y":-0.5}""", line);
        Assert.Equal(new MoveCommand(0.707, -0.5), GameCommands.Parse(line));
    }

    [Fact]
    public void TheGameSaysWhetherItCanBeSteered()
    {
        Assert.Equal(new AbilitiesReport(true), GameReport.Parse(new AbilitiesReport(true).Write()));
        Assert.Equal(new AbilitiesReport(false), GameReport.Parse("""{"protocol":1,"type":"abilities"}"""));
    }

    [Fact]
    public void TheGameReportsWhichCameraItShows()
    {
        CameraReport report = Assert.IsType<CameraReport>(GameReport.Parse("""{"protocol":1,"type":"camera","third":true}"""));

        Assert.True(report.ThirdPerson);
        Assert.Equal(report, GameReport.Parse(report.Write()));
    }
}
