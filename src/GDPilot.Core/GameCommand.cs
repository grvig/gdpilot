using System.Text.Json.Nodes;

namespace GDPilot.Core;

/// <summary>
/// Something GDPilot asks of the in-game part, the other way along the same
/// pipe as <see cref="GameReport"/>: one line of JSON with the same protocol
/// number. The in-game part never reads the pad itself, so everything the pad
/// does to the camera arrives as one of these.
/// </summary>
public abstract record GameCommand
{
    public abstract string Write();

    protected static JsonObject Start(string type)
    {
        return new JsonObject { ["protocol"] = GameReport.Protocol, ["type"] = type };
    }
}

/// <summary>Switches between the game's own camera and the third-person one.</summary>
public sealed record ToggleCameraCommand : GameCommand
{
    public override string Write()
    {
        return Start("toggle-camera").ToJsonString();
    }
}

/// <summary>
/// How fast to turn the third-person camera, in radians per second, until the
/// next look command. Zero stops it. Sent when the rate changes, not every tick.
/// </summary>
public sealed record LookCommand(double YawPerSecond, double PitchPerSecond) : GameCommand
{
    public override string Write()
    {
        JsonObject root = Start("look");
        root["yaw"] = Math.Round(YawPerSecond, 4);
        root["pitch"] = Math.Round(PitchPerSecond, 4);
        return root.ToJsonString();
    }
}

/// <summary>Which camera the game is showing, reported whenever it changes.</summary>
public sealed record CameraReport(bool ThirdPerson) : GameReport;

/// <summary>Reads a command line back, for tests and for anything replaying a session.</summary>
public static class GameCommands
{
    public static GameCommand? Parse(string line)
    {
        JsonObject root = JsonNode.Parse(line)?.AsObject() ?? throw new FormatException("An empty command.");

        switch (root["type"]?.GetValue<string>())
        {
            case "toggle-camera":
                return new ToggleCameraCommand();
            case "look":
                return new LookCommand(root["yaw"]?.GetValue<double>() ?? 0, root["pitch"]?.GetValue<double>() ?? 0);
            default:
                return null;
        }
    }
}
