namespace GDPilot.Core;

/// <summary>Everything the mapper needs besides the button tables.</summary>
public sealed record MappingSettings(
    double LeftDeadzone,
    double RightDeadzone,
    double WalkThreshold,
    DirectionKeys Movement,
    CursorSettings Cursor,
    double LookRadiansPerSecond = 2.5,
    double ZoomBlendPerSecond = 0.6,
    double FollowRadiansPerSecond = 0);

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
    private KeyCode? casting;
    private TimeSpan castHeldFor;

    /// <summary>The skill wheel, or null when no slots are configured.</summary>
    public RadialMenu? Wheel { get; set; }
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

        // A wheel open when input stopped must not cast when it resumes.
        Wheel?.Close();
        casting = null;
    }

    /// <summary>
    /// Opens the wheel while its button is held and adds the cast slot's key for
    /// one tap's length after it is let go. Returns whether the wheel is open.
    /// </summary>
    private bool TickWheel(GamepadSnapshot snapshot, StickPosition right, TimeSpan elapsed, HashSet<KeyCode> keys)
    {
        if (Wheel is not null)
        {
            bool held = false;

            foreach ((GamepadButtons button, ButtonBinding binding, bool chorded) in Buttons.Entries)
            {
                if (binding is RadialBinding && !chorded && snapshot.IsPressed(button))
                {
                    held = true;
                }
            }

            if (Wheel.Update(held, right) is KeyCode cast)
            {
                casting = cast;
                castHeldFor = TimeSpan.Zero;
            }
        }

        if (casting is KeyCode tapping)
        {
            keys.Add(tapping);
            castHeldFor += elapsed;

            if (castHeldFor >= ModeController.TapLength)
            {
                casting = null;
            }
        }

        return Wheel is not null && Wheel.Open;
    }

    /// <summary>
    /// With the right stick left alone, the camera turns gently toward where the
    /// left stick walks, the way an action game's camera settles behind its hero.
    /// Walking straight ahead or backing off turns nothing, and the turn is
    /// strongest walking sideways.
    /// </summary>
    private double Follow(StickPosition left)
    {
        if (settings.FollowRadiansPerSecond <= 0 || left.Magnitude < settings.WalkThreshold)
        {
            return 0;
        }

        double angle = Math.Atan2(left.X, left.Y);

        if (Math.Abs(angle) < 0.3 || Math.Abs(angle) > 2.4)
        {
            return 0;
        }

        return Math.Sin(angle) * settings.FollowRadiansPerSecond;
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
        bool wheelOpen = TickWheel(snapshot, right, elapsed, keys);

        List<InputIntent> intents = differ.Update(keys, pointerButtons);

        // While the wheel is open the right stick belongs to it alone.
        if (wheelOpen)
        {
            Look(0, 0, intents);
            return intents;
        }

        if (Looking)
        {
            if (!aimed && AimPoint is (int x, int y))
            {
                intents.Add(new PointerMoveTo(x, y));
            }

            aimed = true;
            double yaw = right.X * settings.LookRadiansPerSecond;

            if (right.Magnitude == 0)
            {
                yaw = Follow(left);
            }

            Look(yaw, right.Y * settings.ZoomBlendPerSecond, intents);
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
