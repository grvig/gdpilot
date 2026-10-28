namespace GDPilot.Core;

/// <summary>Reads the action half of a binding: "key:Space", "mouse:left", "panel:Inventory" or "back".</summary>
internal static class BindingAction
{
    internal static ButtonBinding Parse(string action, string trigger)
    {
        if (string.Equals(action.Trim(), "back", StringComparison.OrdinalIgnoreCase))
        {
            return new BackBinding();
        }

        string[] parts = action.Split(':', 2, StringSplitOptions.TrimEntries);

        if (parts.Length != 2)
        {
            throw new FormatException($"\"{trigger}\": \"{action}\" should read kind:target, such as key:Space.");
        }

        switch (parts[0].ToLowerInvariant())
        {
            case "key":
                return new KeyBinding(Key(parts[1], trigger));
            case "mouse":
                return Mouse(parts[1], trigger);
            case "panel":
                return new PanelBinding(Panel(parts[1], trigger));
            case "grid":
                return new GridBinding(GridAction(parts[1], trigger));
            case "tab":
                return new TabBinding(TabStep(parts[1], trigger));
            case "camera" when string.Equals(parts[1], "toggle", StringComparison.OrdinalIgnoreCase):
                return new CameraToggleBinding();
            case "radial" when string.Equals(parts[1], "hotbar", StringComparison.OrdinalIgnoreCase):
                return new RadialBinding();
            case "lock" when string.Equals(parts[1], "toggle", StringComparison.OrdinalIgnoreCase):
                return new LockBinding();
            default:
                throw new FormatException($"\"{trigger}\": \"{parts[0]}\" bindings are not supported.");
        }
    }

    private static int TabStep(string name, string trigger)
    {
        switch (name.ToLowerInvariant())
        {
            case "next":
                return 1;
            case "previous":
                return -1;
            default:
                throw new FormatException($"\"{trigger}\": \"{name}\" is not a tab action. Use next or previous.");
        }
    }

    private static string GridAction(string name, string trigger)
    {
        string action = name.ToLowerInvariant();

        if (action is not ("up" or "down" or "left" or "right" or "next"))
        {
            throw new FormatException($"\"{trigger}\": \"{name}\" is not a grid action. Use up, down, left, right or next.");
        }

        return action;
    }

    internal static Mode Panel(string name, string trigger)
    {
        bool known = Enum.TryParse(name, true, out Mode mode) && Enum.IsDefined(mode);

        if (!known || mode is Mode.Combat or Mode.Suspended || char.IsDigit(name[0]))
        {
            throw new FormatException($"\"{trigger}\": \"{name}\" is not a panel.");
        }

        return mode;
    }

    internal static KeyCode Key(string name, string trigger)
    {
        // The plan's own example writes number keys as bare digits.
        if (name.Length == 1 && char.IsDigit(name[0]))
        {
            return KeyCode.D0 + (name[0] - '0');
        }

        KeyChord chord = KeyChord.Parse(name);

        // A binding holds one key. Dropping a written modifier silently would
        // make the binding do something other than what the file says.
        if (chord.Modifiers != KeyModifiers.None)
        {
            throw new FormatException($"\"{trigger}\": \"{name}\" holds modifiers; a binding holds a single key.");
        }

        return chord.Key;
    }

    /// <summary>"left", "right", or either with one modifier, such as "ctrl+left" to split a stack.</summary>
    private static PointerBinding Mouse(string target, string trigger)
    {
        string[] parts = target.Split('+', StringSplitOptions.TrimEntries);

        if (parts.Length == 1)
        {
            return new PointerBinding(Pointer(parts[0], trigger));
        }

        if (parts.Length != 2)
        {
            throw new FormatException($"\"{trigger}\": \"{target}\" holds more than one modifier.");
        }

        KeyCode modifier;

        switch (parts[0].ToLowerInvariant())
        {
            case "ctrl":
            case "control":
                modifier = KeyCode.LeftControl;
                break;
            case "shift":
                modifier = KeyCode.LeftShift;
                break;
            case "alt":
                modifier = KeyCode.LeftAlt;
                break;
            default:
                throw new FormatException($"\"{trigger}\": \"{parts[0]}\" is not a modifier. Use Ctrl, Shift or Alt.");
        }

        return new PointerBinding(Pointer(parts[1], trigger), modifier);
    }

    private static PointerButton Pointer(string name, string trigger)
    {
        switch (name.ToLowerInvariant())
        {
            case "left":
                return PointerButton.Left;
            case "right":
                return PointerButton.Right;
            default:
                throw new FormatException($"\"{trigger}\": \"{name}\" is not a mouse button. Use left or right.");
        }
    }
}
