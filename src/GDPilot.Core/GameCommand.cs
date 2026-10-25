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
/// How fast to turn the third-person camera, in radians per second, to zoom it,
/// in the game's zoom blend per second (positive is closer), and to tilt it, in
/// degrees per second (positive looks up), until the next look command. The
/// game tilts the camera with its zoom as well, so zooming in also levels the
/// view. Zero stops each. Sent when a rate changes, not every tick.
/// </summary>
public sealed record LookCommand(double YawPerSecond, double ZoomPerSecond, double TiltPerSecond = 0) : GameCommand
{
    public override string Write()
    {
        JsonObject root = Start("look");
        root["yaw"] = Math.Round(YawPerSecond, 4);
        root["zoom"] = Math.Round(ZoomPerSecond, 4);

        if (TiltPerSecond != 0)
        {
            root["tilt"] = Math.Round(TiltPerSecond, 2);
        }

        return root.ToJsonString();
    }
}

/// <summary>
/// The enemy to keep the third-person camera turned toward, by the id the game
/// reported it under, until the next lock command. Zero lets the camera go.
/// </summary>
public sealed record LockCommand(uint Id) : GameCommand
{
    public override string Write()
    {
        JsonObject root = Start("lock");
        root["id"] = Id;
        return root.ToJsonString();
    }
}

/// <summary>
/// Swings the third-person camera round behind the character, the way action
/// games answer a lock-on press with nothing to lock onto.
/// </summary>
public sealed record RecenterCommand : GameCommand
{
    public override string Write()
    {
        return Start("recenter").ToJsonString();
    }
}

/// <summary>
/// The left stick, for the in-game part to steer the character with in any
/// direction: up is the camera's forward, each axis from -1 to 1. Zero lets the
/// game's own keys steer again. A held stick is sent again several times a
/// second, since the in-game part stops steering on its own after half a second
/// of silence, so a stalled GDPilot can never leave the character walking.
/// </summary>
public sealed record MoveCommand(double X, double Y) : GameCommand
{
    public override string Write()
    {
        JsonObject root = Start("move");
        root["x"] = Math.Round(X, 3);
        root["y"] = Math.Round(Y, 3);
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
                return new LookCommand(root["yaw"]?.GetValue<double>() ?? 0, root["zoom"]?.GetValue<double>() ?? 0, root["tilt"]?.GetValue<double>() ?? 0);
            case "lock":
                return new LockCommand(root["id"]?.GetValue<uint>() ?? 0);
            case "recenter":
                return new RecenterCommand();
            case "move":
                return new MoveCommand(root["x"]?.GetValue<double>() ?? 0, root["y"]?.GetValue<double>() ?? 0);
            default:
                return null;
        }
    }
}
