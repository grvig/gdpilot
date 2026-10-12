namespace GDPilot.Core;

[Flags]
public enum KeyModifiers
{
    None = 0,
    Control = 1,
    Alt = 2,
    Shift = 4,
}

/// <summary>A key plus the modifiers held with it, such as the kill switch "Ctrl+Alt+Pause".</summary>
public readonly record struct KeyChord(KeyModifiers Modifiers, KeyCode Key)
{
    public static KeyChord Parse(string text)
    {
        string[] parts = text.Split('+', StringSplitOptions.TrimEntries);

        if (parts.Any(part => part.Length == 0))
        {
            throw new FormatException($"\"{text}\" has an empty part.");
        }

        KeyModifiers modifiers = KeyModifiers.None;

        for (int index = 0; index < parts.Length - 1; index++)
        {
            KeyModifiers modifier = ModifierNamed(parts[index]);

            if ((modifiers & modifier) != 0)
            {
                throw new FormatException($"\"{text}\" names {parts[index]} twice.");
            }

            modifiers |= modifier;
        }

        // TryParse also accepts a bare number as a raw key code, so "Ctrl+19" would
        // quietly mean Pause. A key name never starts with a digit: the 1 key is D1.
        bool known = Enum.TryParse(parts[^1], true, out KeyCode key) && Enum.IsDefined(key);

        if (!known || key == KeyCode.None || char.IsDigit(parts[^1][0]))
        {
            throw new FormatException($"\"{parts[^1]}\" in \"{text}\" is not a key.");
        }

        return new KeyChord(modifiers, key);
    }

    /// <summary>
    /// Extra modifiers still match. For a kill switch, firing when someone also
    /// happens to hold Shift is the safe mistake; refusing to fire is not.
    /// </summary>
    public bool Matches(KeyCode pressed, KeyModifiers held)
    {
        return pressed == Key && (held & Modifiers) == Modifiers;
    }

    private static KeyModifiers ModifierNamed(string name)
    {
        switch (name.ToLowerInvariant())
        {
            case "ctrl":
            case "control":
                return KeyModifiers.Control;
            case "alt":
                return KeyModifiers.Alt;
            case "shift":
                return KeyModifiers.Shift;
            default:
                throw new FormatException($"\"{name}\" is not a modifier. Use Ctrl, Alt or Shift.");
        }
    }
}
