namespace GDPilot.Core;

/// <summary>
/// Everything the overlay shows, worked out here so it can be tested. The window
/// only draws it, and how a window draws cannot be asserted.
/// </summary>
public sealed record OverlayState(string Mode, string? Suspended, IReadOnlyList<string> Legend)
{
    /// <summary>The skill wheel to draw over the game, or null while it is closed.</summary>
    public WheelView? Wheel { get; init; }

    /// <summary>The marker over the locked enemy, or null with nothing locked or the enemy out of view.</summary>
    public LockMarker? Marker { get; init; }

    /// <summary>
    /// The state with nothing drawn over the game's middle while input is
    /// suspended: alt-tabbed away, the wheel and the marker would otherwise float
    /// over whatever window is in front.
    /// </summary>
    public OverlayState Floating()
    {
        if (Suspended is null)
        {
            return this;
        }

        return this with { Wheel = null, Marker = null };
    }

    /// <param name="gridFellBack">Whether grid presses are stepping because the screen could not be read, said first so it is noticed.</param>
    /// <param name="thirdPerson">Whether the third-person camera is on, which changes what the right stick does.</param>
    /// <param name="locked">The enemy held by lock-on, which changes it again.</param>
    public static OverlayState Build(Mode mode, SuspendReason reasons, ButtonLayers layers, bool gridFellBack = false, bool thirdPerson = false, SeenTarget? locked = null)
    {
        List<string> legend = new();

        if (locked is not null)
        {
            legend.Add($"Locked on: {locked.Health}% life; flick the right stick to switch");
        }
        else if (thirdPerson)
        {
            legend.Add("Camera: third person; right stick turns and zooms");
        }

        if (gridFellBack)
        {
            legend.Add("Grid: stepping, screen unclear");
        }

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
            case CameraToggleBinding:
                return "Switch camera";
            case RadialBinding:
                return "Skill wheel";
            case LockBinding:
                return "Lock on";
            case DodgeBinding dodge:
                return $"Dodge ({Describe(new KeyBinding(dodge.Key))})";
            default:
                return binding.ToString();
        }
    }
}

/// <summary>Where to draw the lock-on marker, in screen pixels, and the life of the enemy under it, in percent.</summary>
public sealed record LockMarker(int X, int Y, int Health)
{
    /// <summary>
    /// The marker for a locked enemy at the point the cursor follows, so the two
    /// glide together; null when there is none to draw: nothing locked, out of
    /// view, or no game window.
    /// </summary>
    public static LockMarker? For(SeenTarget? target, (int X, int Y)? spot)
    {
        if (target is null || !target.OnScreen || spot is not (int x, int y))
        {
            return null;
        }

        return new LockMarker(x, y, target.Health);
    }
}
