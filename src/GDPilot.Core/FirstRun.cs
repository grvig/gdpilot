namespace GDPilot.Core;

/// <summary>
/// What a person is told before they first play, each thing either ready or
/// saying what to do about it. Nothing here stops GDPilot starting, since each
/// can be put right while it runs.
/// </summary>
public static class FirstRun
{
    public readonly record struct Facts(bool PadConnected, bool GameRunning, bool SettingsRead, string? Display, bool FunctionsFound, string? Settings = null, IReadOnlyList<string>? KeyMismatches = null);

    /// <summary>The game's movement type "Keyboard" in its options.txt, the one GDPilot's keys walk with.</summary>
    private const string KeyboardMovement = "1";

    /// <summary>The game's display mode "Borderless Window" in its options.txt, over which a window can be drawn.</summary>
    private const string BorderlessScreen = "2";

    public static IReadOnlyList<(bool Ready, string Line)> Checks(Facts facts)
    {
        List<(bool Ready, string Line)> checks = new();

        if (facts.PadConnected)
        {
            checks.Add((true, "A controller is connected."));
        }
        else
        {
            checks.Add((false, "No controller found. Connect one; GDPilot picks it up as soon as it appears."));
        }

        // What the game saved last time can be read with it closed, and a new player is told to start GDPilot first.
        if (facts.Settings is string settings)
        {
            if (GameSettings.Value(settings, "gamepadSupport") == "true")
            {
                checks.Add((false, "The game's own controller support is on, and would answer the pad as well. Turn off Gamepad in its options."));
            }

            if (GameSettings.Value(settings, "screenMode") is string screen && screen != BorderlessScreen)
            {
                checks.Add((false, "The game's display mode should be Borderless Window: GDPilot's card and wheel cannot show over full screen. Change it in its options."));
            }

            if (GameSettings.Value(settings, "movementType") is string movement && movement != KeyboardMovement)
            {
                checks.Add((false, "The game's movement type should be Keyboard, not Keyboard Only or Mouse. Change it in its options."));
            }
        }

        if (facts.KeyMismatches is { Count: > 0 } mismatches)
        {
            checks.Add((false, $"In the game's keybinding options, set {string.Join(", ", mismatches)}: these are the keys GDPilot presses for them."));
        }

        if (facts.GameRunning)
        {
            checks.Add((true, "Grim Dawn is running."));

            if (facts.SettingsRead && facts.Display is not null)
            {
                checks.Add((true, $"The game runs at {facts.Display}, and its menus are placed for that."));
            }
            else
            {
                checks.Add((false, "The game's options.txt could not be read, so its menus cannot be placed. Change any setting in the game's options once to have it written."));
            }

            if (facts.FunctionsFound)
            {
                checks.Add((true, "This version of the game has everything the in-game part needs."));
            }
            else
            {
                checks.Add((false, "This version of the game is missing something the in-game part needs. The camera and lock-on stay off until GDPilot is updated; everything else works."));
            }
        }
        else
        {
            checks.Add((false, "Grim Dawn is not running. Start it whenever you like; GDPilot connects by itself."));
        }

        return checks;
    }
}
