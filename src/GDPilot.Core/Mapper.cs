namespace GDPilot.Core;

/// <summary>Everything the mapper needs besides the button tables.</summary>
public sealed record MappingSettings(
    double LeftDeadzone,
    double RightDeadzone,
    double WalkThreshold,
    DirectionKeys Movement,
    CursorSettings Cursor);

/// <summary>
/// One tick of the whole controller mapping: shape both sticks, decide what
/// should be held, turn that into presses and releases, then add the cursor
/// move. It never touches hardware, so a replayed trace drives it exactly as
/// the real pad would.
/// </summary>
public sealed class Mapper
{
    private readonly MappingSettings settings;
    private readonly HeldInputDiffer differ = new();
    private readonly CursorMotion cursor;

    public Mapper(MappingSettings settings, ButtonLayers buttons)
    {
        this.settings = settings;
        Buttons = buttons;
        cursor = new CursorMotion(settings.Cursor);
    }

    /// <summary>The held bindings for the current mode. The control loop swaps it as the mode changes.</summary>
    public ButtonLayers Buttons { get; set; }

    /// <summary>Where the left stick rests, from calibration.</summary>
    public StickPosition LeftCentre { get; set; } = StickPosition.Centre;

    /// <summary>Where the right stick rests, from calibration.</summary>
    public StickPosition RightCentre { get; set; } = StickPosition.Centre;

    /// <summary>
    /// Forgets held state after a suspension has released everything, so a
    /// stick still pushed on resume presses its keys again.
    /// </summary>
    public void Reset()
    {
        differ.Forget();
        cursor.Reset();
    }

    public List<InputIntent> Tick(GamepadSnapshot snapshot, TimeSpan elapsed)
    {
        StickPosition left = StickShaping.ApplyRadialDeadzone(snapshot.LeftStick, LeftCentre, settings.LeftDeadzone);
        StickPosition right = StickShaping.ApplyRadialDeadzone(snapshot.RightStick, RightCentre, settings.RightDeadzone);

        (HashSet<KeyCode> keys, HashSet<PointerButton> pointerButtons) = Buttons.Resolve(snapshot);
        keys.UnionWith(StickDirections.KeysFor(left, settings.WalkThreshold, settings.Movement));

        List<InputIntent> intents = differ.Update(keys, pointerButtons);
        PointerMove? move = cursor.Step(right, elapsed);

        if (move is not null)
        {
            intents.Add(move);
        }

        return intents;
    }
}
