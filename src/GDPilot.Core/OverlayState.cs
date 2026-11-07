namespace GDPilot.Core;

/// <summary>
/// Everything the overlay shows, worked out here so it can be tested. The window
/// only draws it, and how a window draws cannot be asserted.
/// </summary>
public sealed record OverlayState(string Mode, string? Suspended, IReadOnlyList<string> Legend)
{
    /// <summary>The mode the card describes, so it can be placed clear of that mode's panels.</summary>
    public Mode Showing { get; init; } = Core.Mode.Combat;

    /// <summary>How many of the legend's first lines say what is going on rather than what a button does.</summary>
    public int Statuses { get; init; }

    /// <summary>
    /// True once the buttons have been on show for a while: the card then folds
    /// down to its title and status lines, so it covers as little of the game as
    /// it can. A change of mode opens it out again.
    /// </summary>
    public bool Folded { get; init; }

    /// <summary>The lines the card shows: all of them, or only the status lines while folded.</summary>
    public IReadOnlyList<string> Shown
    {
        get
        {
            if (Folded)
            {
                return Legend.Take(Statuses).ToList();
            }

            return Legend;
        }
    }
    /// <summary>The skill wheel to draw over the game, or null while it is closed.</summary>
    public WheelView? Wheel { get; init; }

    /// <summary>The marker over the locked enemy, or null with nothing locked or the enemy out of view.</summary>
    public LockMarker? Marker { get; init; }

    /// <summary>What the interact button would do, such as "A  Pick up", or null with nothing in reach.</summary>
    public string? Prompt { get; init; }

    /// <summary>The prompt for a thing on offer, named by the button that would act on it; null with nothing offered or no such button.</summary>
    public static string? PromptFor(NearbyThing? offered, ButtonLayers layers, PadStyle style = PadStyle.Xbox)
    {
        if (offered is null)
        {
            return null;
        }

        foreach ((GamepadButtons button, ButtonBinding binding, bool chorded) in layers.Entries)
        {
            if (binding is InteractBinding && !chorded)
            {
                return $"{ButtonName(button, style)}  {SmartInteract.Verb(offered.Kind)}";
            }
        }

        return null;
    }

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

        return this with { Wheel = null, Marker = null, Prompt = null };
    }

    /// <param name="gridFellBack">Whether grid presses are stepping because the screen could not be read, said first so it is noticed.</param>
    /// <param name="thirdPerson">Whether the third-person camera is on, which changes what the right stick does.</param>
    /// <param name="locked">The enemy held by lock-on, which changes it again.</param>
    /// <param name="steering">Whether the left stick walks in any direction through the game, rather than in eight.</param>
    /// <param name="tilting">Whether the right stick's up and down tilts the view, with the left trigger to zoom.</param>
    public static OverlayState Build(Mode mode, SuspendReason reasons, ButtonLayers layers, bool gridFellBack = false, bool thirdPerson = false, SeenTarget? locked = null, bool steering = false, bool tilting = false, PadStyle style = PadStyle.Xbox, bool padMissing = false)
    {
        List<string> legend = new();

        // First, since nothing else on the card works without it.
        if (padMissing)
        {
            legend.Add("No controller: connect one to play");
        }

        if (locked is not null)
        {
            legend.Add($"Locked on: {locked.Health}% life; flick the right stick to switch");
        }
        else if (thirdPerson && tilting)
        {
            legend.Add("Camera: third person; right stick turns and tilts, LT zooms");
        }
        else if (thirdPerson)
        {
            legend.Add("Camera: third person; right stick turns and zooms");
        }

        if (steering)
        {
            legend.Add("Left stick: walks in any direction");
        }

        if (gridFellBack)
        {
            legend.Add("Grid: stepping, screen unclear");
        }

        int statuses = legend.Count;

        foreach ((GamepadButtons button, ButtonBinding binding, bool chorded) in layers.Entries)
        {
            string trigger = ButtonName(button, style);

            if (chorded)
            {
                trigger = PadGlyphs.Trigger(style) + "+" + trigger;
            }

            legend.Add($"{trigger}: {Describe(binding)}");
        }

        return new OverlayState(Title(mode), SuspendedText(reasons), legend) { Showing = mode, Statuses = statuses };
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

    /// <summary>A button as players call it, as short as the card can carry: LB, R3, View, D-pad ↑.</summary>
    public static string ButtonName(GamepadButtons button, PadStyle style = PadStyle.Xbox)
    {
        return PadGlyphs.Name(button, style);
    }

    /// <summary>A mode's name as a player would say it.</summary>
    public static string Title(Core.Mode mode)
    {
        switch (mode)
        {
            case Core.Mode.Transfer:
                return "Transfer Stash";
            case Core.Mode.WorldMap:
                return "World Map";
            case Core.Mode.LootFilter:
                return "Loot Filter";
            case Core.Mode.MainMenu:
                return "Main Menu";
            case Core.Mode.Dialog:
                return "Conversation";
            case Core.Mode.Confirm:
                return "Question";
            case Core.Mode.Assign:
                return "Hotbar Slot";
            case Core.Mode.Vendor:
                return "Trader";
            case Core.Mode.Service:
                return "Townsfolk";
            default:
                return mode.ToString();
        }
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
                return Title(panel.Mode);
            // The same presses walk bags, skill trees, stars and replies, so they are named for the move, not the cell.
            case GridBinding grid when grid.Action == "next":
                return "Next list";
            case GridBinding grid:
                return $"Move {grid.Action}";
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
            case InteractBinding interact:
                return $"Interact, else {Describe(new KeyBinding(interact.Fallback))}";
            case DodgeBinding dodge:
                return $"Dodge ({Describe(new KeyBinding(dodge.Key))})";
            case RiftBinding:
                return "Rift: travel from here, and back";
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
