using System.Text.Json;
using System.Text.Json.Serialization;

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

        // Lets the overlay corner be written as "TopRight" rather than a number.
        Converters = { new JsonStringEnumConverter() },
    };

    public string KillSwitch { get; init; } = "Ctrl+Alt+Pause";
    public string GameProcess { get; init; } = "Grim Dawn";
    public InputConfig Input { get; init; } = new();
    public OverlayConfig Overlay { get; init; } = new();

    /// <summary>Which entry of <see cref="Profiles"/> holds the grids to navigate with.</summary>
    public string ActiveProfile { get; init; } = string.Empty;

    /// <summary>Grid calibrations, named by resolution and UI scale, for example "1920x1080@0.5".</summary>
    public Dictionary<string, ProfileConfig> Profiles { get; init; } = new();
    public Dictionary<string, Dictionary<string, string>> Bindings { get; init; } = new();

    /// <summary>
    /// The game's own key for each panel the tool can open. Stash, Vendor and
    /// Dialog have none, because the game opens them by clicking a character or
    /// object, so binding one only declares that it is now open.
    ///
    /// Character is absent on purpose: in Grim Dawn the character sheet and the
    /// inventory are one window, bound to both C and I, and tracking them as two
    /// modes would desync the moment one key toggled the window the other opened.
    /// Skills is on K because keyboard movement needs S for walking backward.
    /// </summary>
    public Dictionary<string, string> Panels { get; init; } = new()
    {
        ["Inventory"] = "I",
        ["Skills"] = "K",
        ["Devotion"] = "N",
        ["WorldMap"] = "Tab",
    };

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

    /// <summary>
    /// The button tables for one mode. A mode with no section of its own uses
    /// Combat's, since a panel bound to nothing would leave no way back out of it.
    /// </summary>
    public ButtonLayers ButtonsFor(string mode)
    {
        Dictionary<string, string>? section;

        if (!Bindings.TryGetValue(mode, out section) && !Bindings.TryGetValue(nameof(Mode.Combat), out section))
        {
            section = new Dictionary<string, string>();
        }

        return BindingTable.Parse(section, Input.ChordTrigger);
    }

    public Dictionary<Mode, KeyCode> PanelKeys()
    {
        Dictionary<Mode, KeyCode> keys = new();

        foreach ((string panel, string key) in Panels)
        {
            string entry = $"panels.{panel}";
            keys[BindingAction.Panel(panel, entry)] = BindingAction.Key(key, entry);
        }

        return keys;
    }

    private void Validate()
    {
        KeyChord.Parse(KillSwitch);
        PanelKeys();

        foreach (string mode in Bindings.Keys)
        {
            Require(Enum.TryParse(mode, false, out Mode parsed) && parsed != Mode.Suspended, $"bindings.{mode} is not a mode.");
        }

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
