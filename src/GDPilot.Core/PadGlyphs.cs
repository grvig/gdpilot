namespace GDPilot.Core;

/// <summary>Which family of pad is connected, for naming its buttons as they are printed on it.</summary>
public enum PadStyle
{
    Xbox,
    PlayStation,
    Nintendo,
}

/// <summary>
/// The buttons as each family prints them. GDPilot's buttons go by place: A is
/// always the south button, whatever is printed on it, so a Nintendo pad's south
/// button reads B and a PlayStation pad's reads a cross.
/// </summary>
public static class PadGlyphs
{
    /// <summary>The family from what SDL calls the pad's type, then its name; anything unknown reads as Xbox, the commonest layout.</summary>
    public static PadStyle StyleOf(string type, string? name)
    {
        string said = (type + " " + name).ToUpperInvariant();

        if (said.Contains("PS3") || said.Contains("PS4") || said.Contains("PS5") || said.Contains("PLAYSTATION") || said.Contains("DUALSENSE") || said.Contains("DUALSHOCK"))
        {
            return PadStyle.PlayStation;
        }

        if (said.Contains("NINTENDO") || said.Contains("SWITCH") || said.Contains("GAMECUBE") || said.Contains("JOYCON"))
        {
            return PadStyle.Nintendo;
        }

        return PadStyle.Xbox;
    }

    /// <summary>A setting's word for the family, or null for "auto" or anything else.</summary>
    public static PadStyle? Chosen(string? setting)
    {
        switch (setting?.ToLowerInvariant())
        {
            case "xbox":
                return PadStyle.Xbox;
            case "playstation":
                return PadStyle.PlayStation;
            case "nintendo":
                return PadStyle.Nintendo;
            default:
                return null;
        }
    }

    public static string Name(GamepadButtons button, PadStyle style)
    {
        switch (button)
        {
            case GamepadButtons.DPadUp:
                return "D-pad ↑";
            case GamepadButtons.DPadDown:
                return "D-pad ↓";
            case GamepadButtons.DPadLeft:
                return "D-pad ←";
            case GamepadButtons.DPadRight:
                return "D-pad →";
            case GamepadButtons.LeftStick:
                return "L3";
            case GamepadButtons.RightStick:
                return "R3";
        }

        switch (style)
        {
            case PadStyle.PlayStation:
                return PlayStation(button);
            case PadStyle.Nintendo:
                return Nintendo(button);
            default:
                return Xbox(button);
        }
    }

    /// <summary>What the left trigger reads as, before a chord's button.</summary>
    public static string Trigger(PadStyle style)
    {
        switch (style)
        {
            case PadStyle.PlayStation:
                return "L2";
            case PadStyle.Nintendo:
                return "ZL";
            default:
                return "LT";
        }
    }

    /// <summary>The colour a face button's name is printed in on the pad, as RGB hex, or null for any other button.</summary>
    public static string? Colour(string name)
    {
        switch (name)
        {
            case "A" or "✕":
                return ColourFor(name, "#6FBF4A", "#7A9BE0");
            case "B" or "○":
                return "#E0524A";
            case "X" or "□":
                return ColourFor(name, "#4A8FE0", "#D88BC0");
            case "Y" or "△":
                return ColourFor(name, "#E8C13A", "#4FC0A0");
            default:
                return null;
        }
    }

    private static string ColourFor(string name, string letter, string shape)
    {
        if (name.Length == 1 && char.IsLetter(name[0]))
        {
            return letter;
        }

        return shape;
    }

    private static string Xbox(GamepadButtons button)
    {
        switch (button)
        {
            case GamepadButtons.LeftBumper:
                return "LB";
            case GamepadButtons.RightBumper:
                return "RB";
            case GamepadButtons.Back:
                return "View";
            case GamepadButtons.Start:
                return "Menu";
            default:
                return button.ToString();
        }
    }

    private static string PlayStation(GamepadButtons button)
    {
        switch (button)
        {
            case GamepadButtons.A:
                return "✕";
            case GamepadButtons.B:
                return "○";
            case GamepadButtons.X:
                return "□";
            case GamepadButtons.Y:
                return "△";
            case GamepadButtons.LeftBumper:
                return "L1";
            case GamepadButtons.RightBumper:
                return "R1";
            case GamepadButtons.Back:
                return "Create";
            case GamepadButtons.Start:
                return "Options";
            default:
                return button.ToString();
        }
    }

    private static string Nintendo(GamepadButtons button)
    {
        switch (button)
        {
            case GamepadButtons.A:
                return "B";
            case GamepadButtons.B:
                return "A";
            case GamepadButtons.X:
                return "Y";
            case GamepadButtons.Y:
                return "X";
            case GamepadButtons.LeftBumper:
                return "L";
            case GamepadButtons.RightBumper:
                return "R";
            case GamepadButtons.Back:
                return "−";
            case GamepadButtons.Start:
                return "+";
            default:
                return button.ToString();
        }
    }
}
