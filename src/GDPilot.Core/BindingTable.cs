namespace GDPilot.Core;

/// <summary>
/// Reads one mode's bindings in the configuration's own words: "A": "key:Space"
/// binds a button, and "LT+A": "key:1" binds it in the left-trigger chord layer.
/// </summary>
public static class BindingTable
{
    private const string ChordPrefix = "LT+";

    public static ButtonLayers Parse(IReadOnlyDictionary<string, string> section, double chordTriggerThreshold)
    {
        Dictionary<GamepadButtons, ButtonBinding> baseLayer = new();
        Dictionary<GamepadButtons, ButtonBinding> chordLayer = new();

        foreach ((string trigger, string action) in section)
        {
            string buttonName = trigger.Trim();
            Dictionary<GamepadButtons, ButtonBinding> layer = baseLayer;

            if (buttonName.StartsWith(ChordPrefix, StringComparison.OrdinalIgnoreCase))
            {
                buttonName = buttonName.Substring(ChordPrefix.Length);
                layer = chordLayer;
            }

            if (!layer.TryAdd(Button(buttonName, trigger), BindingAction.Parse(action, trigger)))
            {
                throw new FormatException($"\"{trigger}\" is bound more than once.");
            }
        }

        return new ButtonLayers(baseLayer, chordLayer, chordTriggerThreshold);
    }

    private static GamepadButtons Button(string name, string trigger)
    {
        // IsDefined rejects combinations such as "A, B", which the parser would accept.
        bool known = Enum.TryParse(name, true, out GamepadButtons button) && Enum.IsDefined(button);

        if (!known || button == GamepadButtons.None || char.IsDigit(name[0]))
        {
            throw new FormatException($"\"{trigger}\" does not name a pad button.");
        }

        return button;
    }
}
