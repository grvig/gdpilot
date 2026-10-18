namespace GDPilot.Core;

/// <summary>Reads the action half of a binding, such as "key:Space" or "mouse:left".</summary>
internal static class BindingAction
{
    internal static ButtonBinding Parse(string action, string trigger)
    {
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
            default:
                throw new FormatException($"\"{trigger}\": \"{parts[0]}\" bindings are not supported.");
        }
    }

    private static KeyCode Key(string name, string trigger)
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
