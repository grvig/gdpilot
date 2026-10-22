namespace GDPilot.Core;

/// <summary>
/// Everything the overlay shows, worked out here so it can be tested. The window
/// only draws it, and how a window draws cannot be asserted.
/// </summary>
public sealed record OverlayState(string Mode, string? Suspended, IReadOnlyList<string> Legend)
{
    public static OverlayState Build(Mode mode, SuspendReason reasons, ButtonLayers layers)
    {
        List<string> legend = new();

        foreach ((GamepadButtons button, ButtonBinding binding, bool chorded) in layers.Entries)
        {
            string trigger = button.ToString();

            if (chorded)
            {
                trigger = "LT+" + trigger;
            }

            legend.Add($"{trigger}: {Describe(binding)}");
        }

        return new OverlayState(mode.ToString(), SuspendedText(reasons), legend);
    }

    /// <summary>Null while injecting, otherwise why not. The kill switch is named first because it needs a press to clear.</summary>
    private static string? SuspendedText(SuspendReason reasons)
    {
        if ((reasons & SuspendReason.KillSwitch) != 0)
        {
            return "Suspended: kill switch";
        }

        if ((reasons & SuspendReason.FocusLost) != 0)
        {
            return "Suspended: game not in front";
        }

        return null;
    }

    private static string ModifierName(KeyCode modifier)
    {
        switch (modifier)
        {
            case KeyCode.LeftControl:
                return "Ctrl";
            case KeyCode.LeftShift:
                return "Shift";
            case KeyCode.LeftAlt:
                return "Alt";
            default:
                return modifier.ToString();
        }
    }

    private static string Describe(ButtonBinding binding)
    {
        switch (binding)
        {
            case KeyBinding key when key.Key is >= KeyCode.D0 and <= KeyCode.D9:
                return ((int)(key.Key - KeyCode.D0)).ToString();
            case KeyBinding key:
                return key.Key.ToString();
            case PointerBinding pointer when pointer.Modifier != KeyCode.None:
                return $"{ModifierName(pointer.Modifier)}+{pointer.Button} click";
            case PointerBinding pointer:
                return $"{pointer.Button} click";
            case PanelBinding panel:
                return panel.Mode.ToString();
            case GridBinding grid when grid.Action == "next":
                return "Next grid";
            case GridBinding grid:
                return $"Cell {grid.Action}";
            case TabBinding tab when tab.Step > 0:
                return "Next tab";
            case TabBinding:
                return "Previous tab";
            case BackBinding:
                return "Back";
            default:
                return binding.ToString();
        }
    }
}
