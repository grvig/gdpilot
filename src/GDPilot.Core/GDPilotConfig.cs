using System.Text.Json;

namespace GDPilot.Core;

/// <summary>
/// The configuration file from section 7. Anything left out takes the default
/// shown here, so a file only needs to state what it changes. Fields not used
/// yet, such as the grid profiles, are read past rather than rejected.
/// </summary>
public sealed class GDPilotConfig
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public string KillSwitch { get; init; } = "Ctrl+Alt+Pause";
    public string GameProcess { get; init; } = "Grim Dawn";
    public InputConfig Input { get; init; } = new();
    public Dictionary<string, Dictionary<string, string>> Bindings { get; init; } = new();

    public static GDPilotConfig Parse(string json)
    {
        GDPilotConfig config = JsonSerializer.Deserialize<GDPilotConfig>(json, Options) ?? new GDPilotConfig();
        config.Validate();
        return config;
    }

    public MappingSettings Mapping()
    {
        CursorSettings cursor = new(Input.Cursor.BaseSpeed, Input.Cursor.Exponent, Input.Cursor.MaxSpeed);
        return new MappingSettings(Input.Deadzone.Left, Input.Deadzone.Right, Input.WalkThreshold, DirectionKeys.Wasd, cursor);
    }

    /// <summary>The button tables for one mode, such as "Combat". A mode with no section binds nothing.</summary>
    public ButtonLayers ButtonsFor(string mode)
    {
        Dictionary<string, string>? section;

        if (!Bindings.TryGetValue(mode, out section))
        {
            section = new Dictionary<string, string>();
        }

        return BindingTable.Parse(section, Input.ChordTrigger);
    }

    private void Validate()
    {
        KeyChord.Parse(KillSwitch);
        Require(Input.PollHz is >= 30 and <= 1000, "input.pollHz must be between 30 and 1000.");
        Require(Input.Deadzone.Left is >= 0 and < 1 && Input.Deadzone.Right is >= 0 and < 1, "Deadzones must be at least 0 and below 1.");
        Require(Input.WalkThreshold is > 0 and <= 1, "input.walkThreshold must be above 0 and at most 1.");
        Require(Input.ChordTrigger is > 0 and <= 1, "input.chordTrigger must be above 0 and at most 1.");
        Require(Input.Cursor.BaseSpeed > 0 && Input.Cursor.Exponent > 0 && Input.Cursor.MaxSpeed > 0, "Cursor speeds and exponent must be positive.");

        foreach (string mode in Bindings.Keys)
        {
            ButtonsFor(mode);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new FormatException(message);
        }
    }
}
