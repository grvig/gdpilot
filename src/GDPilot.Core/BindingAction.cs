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
                return new PointerBinding(Pointer(parts[1], trigger));
            case "panel":
                return new PanelBinding(Panel(parts[1], trigger));
            default:
                throw new FormatException($"\"{trigger}\": \"{parts[0]}\" bindings are not supported.");
        }
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
