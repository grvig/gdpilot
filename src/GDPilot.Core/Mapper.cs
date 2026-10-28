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
    private bool wasLocked;

    /// <summary>Soft aim, drawing the cursor onto an enemy near the aim point in third person; null when turned off.</summary>
    public SoftAim? SoftAim { get; set; }

    /// <summary>Lock-on, or null when it is not set up.</summary>
    public LockOn? Lock { get; set; }

    /// <summary>The enemies the game last reported near the player.</summary>
    public IReadOnlyList<SeenTarget> Seen { get; set; } = Array.Empty<SeenTarget>();

    /// <summary>The game window's client area on the screen, for putting the cursor on a locked enemy; null when unknown.</summary>
    public ScreenRect? GameBounds { get; set; }

    /// <summary>False while a panel is open over the world, which lets go of any lock.</summary>
    public bool InWorld { get; set; } = true;

    /// <summary>What lock-on did on the last tick, for the rumble.</summary>
    public LockChange LockChanged { get; private set; }

    private bool lockHeld;
    private uint sentLock;
    private (int X, int Y)? cursorAt;
    private readonly Glide glide = new();
    private uint glidingId;
    private IReadOnlyList<SeenTarget>? glidedReport;

    /// <summary>Where the locked enemy is drawn this tick, in screen pixels, followed smoothly between reports; null with nothing locked in view.</summary>
    public (int X, int Y)? LockPoint { get; private set; }

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

        // The next tick tells the game the lock has gone.
        Lock?.Release();
        lockHeld = false;
        cursorAt = null;
        wasLocked = false;
    }

    /// <summary>
    /// Locks on as the lock button goes down, follows the lock, and tells the
    /// game whenever the enemy held changes. While the wheel is open the right
    /// stick belongs to it, so it cannot flick the lock elsewhere.
    /// </summary>
    private void TickLock(GamepadSnapshot snapshot, StickPosition right, TimeSpan elapsed, bool wheelOpen, List<InputIntent> intents)
    {
        LockChanged = LockChange.None;

        if (Lock is null)
        {
            return;
        }

        Lock.Aspect = Aspect();

        bool pressed = false;

        foreach ((GamepadButtons button, ButtonBinding binding, bool _) in Buttons.Entries)
        {
            if (binding is LockBinding && snapshot.IsPressed(button) && Buttons.BindingFor(button, snapshot.LeftTrigger) is LockBinding)
            {
                pressed = true;
            }
        }

        if (!InWorld)
        {
            Lock.Release();
        }
        else if (pressed && !lockHeld)
        {
            (double x, double y) = AimFraction();
            LockChanged = Lock.Toggle(Seen, x, y);

            // Nothing to lock onto: the camera swings round behind the character instead.
            if (LockChanged == LockChange.None && Looking)
            {
                intents.Add(new GameCommandIntent(new RecenterCommand()));
            }
        }

        lockHeld = pressed;

        if (LockChanged == LockChange.None)
        {
            StickPosition flick = right;

            if (wheelOpen)
            {
                flick = StickPosition.Centre;
            }

            LockChanged = Lock.Update(Seen, flick, elapsed);
        }

        uint id = 0;

        if (Lock.Target is SeenTarget held)
        {
            id = held.Id;
        }

        if (id != sentLock)
        {
            sentLock = id;
            intents.Add(new GameCommandIntent(new LockCommand(id)));
        }
    }

    /// <summary>Where the player looks, as fractions of the game window: the aim point in third person, else the middle.</summary>
    private (double X, double Y) AimFraction()
    {
        if (Looking && AimPoint is (int x, int y) && GameBounds is ScreenRect bounds && bounds.Width > 0 && bounds.Height > 0)
        {
            return ((double)(x - bounds.X) / bounds.Width, (double)(y - bounds.Y) / bounds.Height);
        }

        return (0.5, 0.5);
    }

    /// <summary>
    /// Where an enemy is drawn this tick, in screen pixels, gliding between the
    /// game's reports; null when it is out of view or the window is unknown.
    /// </summary>
    private (int X, int Y)? Glided(SeenTarget target, TimeSpan elapsed)
    {
        if (target.Id != glidingId)
        {
            glide.Reset();
            glidingId = target.Id;
            glidedReport = null;
        }

        if (!ReferenceEquals(Seen, glidedReport))
        {
            glidedReport = Seen;
            glide.Report(target.X, target.Y);
        }

        (double x, double y) = glide.Advance(elapsed);

        if (!target.OnScreen || GameBounds is not ScreenRect bounds)
        {
            return null;
        }

        return ((int)Math.Round(bounds.X + x * bounds.Width), (int)Math.Round(bounds.Y + y * bounds.Height));
    }

    /// <summary>Puts the cursor on a spot, but only when it has moved a couple of pixels, so a still spot sends nothing.</summary>
    private void Park((int X, int Y)? spot, List<InputIntent> intents)
    {
        if (spot is not (int x, int y))
        {
            return;
        }

        if (cursorAt is (int lastX, int lastY) && Math.Abs(lastX - x) < 2 && Math.Abs(lastY - y) < 2)
        {
            return;
        }

        cursorAt = (x, y);
        intents.Add(new PointerMoveTo(x, y));
    }

    /// <summary>
    /// In third person without a lock, the cursor waits at the aim point, or on
    /// the enemy soft aim has drawn it onto.
    /// </summary>
    private (int X, int Y)? Resting(TimeSpan elapsed)
    {
        if (SoftAim is null)
        {
            return AimPoint;
        }

        (double x, double y) = AimFraction();

        if (SoftAim.Pick(Seen, x, y, Aspect()) is SeenTarget soft && Glided(soft, elapsed) is (int X, int Y) spot)
        {
            return spot;
        }

        return AimPoint;
    }

    private double Aspect()
    {
        if (GameBounds is ScreenRect bounds && bounds.Height > 0)
        {
            return (double)bounds.Width / bounds.Height;
        }

        return 16.0 / 9;
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
        TickLock(snapshot, right, elapsed, wheelOpen, intents);

        // Locked, the right stick only flicks between enemies, the game turns the
        // camera, and the cursor stays on the enemy so attacks and skills land on it.
        if (Lock?.Target is SeenTarget held)
        {
            LockPoint = Glided(held, elapsed);
            Park(LockPoint ?? AimPoint, intents);
            Look(0, 0, intents);
            wasLocked = true;
            return intents;
        }

        // Let go, the cursor is placed again wherever it now belongs.
        LockPoint = null;

        if (wasLocked)
        {
            wasLocked = false;
            cursorAt = null;
        }

        // While the wheel is open the right stick belongs to it alone.
        if (wheelOpen)
        {
            Look(0, 0, intents);
            return intents;
        }

        if (Looking)
        {
            Park(Resting(elapsed), intents);
            double yaw = right.X * settings.LookRadiansPerSecond;

            if (right.Magnitude == 0)
            {
                yaw = Follow(left);
            }

            Look(yaw, right.Y * settings.ZoomBlendPerSecond, intents);
            return intents;
        }

        // Leaving third person with the stick still over must not leave the camera turning.
        cursorAt = null;
        SoftAim?.Forget();
        Look(0, 0, intents);
        PointerMove? move = cursor.Step(right, elapsed);

        if (move is not null)
        {
            intents.Add(move);
        }

        return intents;
    }
}
