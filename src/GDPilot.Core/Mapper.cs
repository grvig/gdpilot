namespace GDPilot.Core;

/// <summary>Everything the mapper needs besides the button tables.</summary>
public sealed record MappingSettings(
    double LeftDeadzone,
    double RightDeadzone,
    double WalkThreshold,
    DirectionKeys Movement,
    CursorSettings Cursor,
    double LookRadiansPerSecond = 2.5,
    double ZoomBlendPerSecond = 0.6);

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
    /// True while the game shows the third-person camera. The right stick then
    /// turns the camera instead of moving the cursor, and because the game's
    /// keyboard movement is relative to the camera, turning it turns "forward".
    /// </summary>
    public bool Looking { get; set; }

    /// <summary>
    /// Where the cursor waits while looking, in screen pixels: a little ahead of
    /// the character, so skills and attacks go where the camera faces. Null
    /// leaves the cursor where it was.
    /// </summary>
    public (int X, int Y)? AimPoint { get; set; }

    private double sentYaw;
    private double sentZoom;
    private bool aimed;

    /// <summary>
    /// Forgets held state after a suspension has released everything, so a
    /// stick still pushed on resume presses its keys again.
    /// </summary>
    public void Reset()
    {
        differ.Forget();
        cursor.Reset();

        // The host stops the camera turning when a suspension begins.
        sentYaw = 0;
        sentZoom = 0;
    }

    /// <summary>Sends turn and zoom rates only when one has changed enough to matter, so the pipe carries changes, not a stream.</summary>
    private void Look(double yaw, double zoom, List<InputIntent> intents)
    {
        if (Same(yaw, sentYaw) && Same(zoom, sentZoom))
        {
            return;
        }

        sentYaw = yaw;
        sentZoom = zoom;
        intents.Add(new GameCommandIntent(new LookCommand(yaw, zoom)));
    }

    /// <summary>Close enough not to resend, though a change to or from stopped always counts.</summary>
    private static bool Same(double rate, double sent)
    {
        return Math.Abs(rate - sent) < 0.02 && (rate == 0) == (sent == 0);
    }

    public List<InputIntent> Tick(GamepadSnapshot snapshot, TimeSpan elapsed)
    {
        StickPosition left = StickShaping.ApplyRadialDeadzone(snapshot.LeftStick, LeftCentre, settings.LeftDeadzone);
        StickPosition right = StickShaping.ApplyRadialDeadzone(snapshot.RightStick, RightCentre, settings.RightDeadzone);

        (HashSet<KeyCode> keys, HashSet<PointerButton> pointerButtons) = Buttons.Resolve(snapshot);
        keys.UnionWith(StickDirections.KeysFor(left, settings.WalkThreshold, settings.Movement));

        List<InputIntent> intents = differ.Update(keys, pointerButtons);

        if (Looking)
        {
            if (!aimed && AimPoint is (int x, int y))
            {
                intents.Add(new PointerMoveTo(x, y));
            }

            aimed = true;
            Look(right.X * settings.LookRadiansPerSecond, right.Y * settings.ZoomBlendPerSecond, intents);
            return intents;
        }

        // Leaving third person with the stick still over must not leave the camera turning.
        aimed = false;
        Look(0, 0, intents);
        PointerMove? move = cursor.Step(right, elapsed);

        if (move is not null)
        {
            intents.Add(move);
        }

        return intents;
    }
}
