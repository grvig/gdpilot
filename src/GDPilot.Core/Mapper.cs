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
    double FollowRadiansPerSecond = 0,
    double TiltDegreesPerSecond = 0);

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
    private double sentTilt;
    private KeyCode? casting;
    private TimeSpan castHeldFor;

    /// <summary>The skill wheel, or null when no slots are configured.</summary>
    public RadialMenu? Wheel { get; set; }
    private bool wasLocked;

    /// <summary>The game's last targets report, for where the character's feet and the ground round it fall on the screen.</summary>
    public TargetsReport? Near { get; set; }

    private bool dodgeHeld;
    private KeyCode? dodging;
    private TimeSpan dodgeHeldFor;

    /// <summary>
    /// True while the in-game part can steer the character: the nearest of WASD's
    /// eight directions is still held, and the stick's exact direction is sent
    /// with it, for the in-game part to turn the walk to any angle.
    /// </summary>
    public bool Steering { get; set; }

    /// <summary>How often a held direction is sent again; the in-game part stops steering after half a second without one.</summary>
    public static readonly TimeSpan MoveRepeat = TimeSpan.FromMilliseconds(150);

    private (double X, double Y) sentMove;
    private TimeSpan sinceMove;

    /// <summary>
    /// Twin-stick aiming in the game's own camera: out in the world the right
    /// stick points the cursor at the ground round the character instead of
    /// pushing it about the screen, so skills go where the stick points, as in a
    /// game made for a controller. Panels keep the free cursor.
    /// </summary>
    public bool TwinStick { get; set; }

    /// <summary>The way twin-stick aiming last pointed, as a screen angle; up the screen to begin with.</summary>
    private double aimAngle = -Math.PI / 2;

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

        // A stick still held on resume steers again at once.
        sentMove = (0, 0);
        sentZoom = 0;
        sentTilt = 0;

        // A wheel open when input stopped must not cast when it resumes.
        Wheel?.Close();
        casting = null;

        // A dodge cut off by the suspension has had its key let go with everything else.
        dodging = null;
        dodgeHeld = false;

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
    /// Sends the stick's direction to steer by, at full length once it leans past
    /// the walk threshold, since the game has one walking speed. Sent when the
    /// direction moves by a degree or so, and again every so often while held,
    /// so the in-game part knows GDPilot is still there.
    /// </summary>
    private void Steer(StickPosition left, TimeSpan elapsed, List<InputIntent> intents)
    {
        (double X, double Y) wanted = (0, 0);

        if (Steering && left.Magnitude >= settings.WalkThreshold)
        {
            wanted = (left.X / left.Magnitude, left.Y / left.Magnitude);
        }

        sinceMove += elapsed;
        bool still = wanted == (0, 0);
        bool wasStill = sentMove == (0, 0);

        if (still && wasStill)
        {
            return;
        }

        double turned = Math.Abs(Math.IEEERemainder(Math.Atan2(wanted.Y, wanted.X) - Math.Atan2(sentMove.Y, sentMove.X), 2 * Math.PI));

        if (still != wasStill || turned > 0.02 || sinceMove >= MoveRepeat)
        {
            sentMove = wanted;
            sinceMove = TimeSpan.Zero;
            intents.Add(new GameCommandIntent(new MoveCommand(Math.Round(wanted.X, 3), Math.Round(wanted.Y, 3))));
        }
    }

    /// <summary>
    /// On a dodge press, puts the cursor on the ground the left stick points at
    /// and taps the skill's key there; once the key is up, the cursor goes back to
    /// wherever it belongs. Returns whether a dodge holds the cursor this tick.
    /// </summary>
    private bool TickDodge(GamepadSnapshot snapshot, StickPosition left, TimeSpan elapsed, List<InputIntent> intents)
    {
        KeyCode? pressedKey = null;

        foreach ((GamepadButtons button, ButtonBinding _, bool _) in Buttons.Entries)
        {
            if (snapshot.IsPressed(button) && Buttons.BindingFor(button, snapshot.LeftTrigger) is DodgeBinding dodge)
            {
                pressedKey = dodge.Key;
            }
        }

        bool pressed = pressedKey is not null;

        if (pressed && !dodgeHeld && dodging is null)
        {
            // Without the game's window the cursor cannot be placed, but the skill still fires.
            if (GameBounds is ScreenRect bounds)
            {
                ScreenSpot spot = DodgeAim.Spot(Near, left, settings.WalkThreshold, Aspect());
                intents.Add(new PointerMoveTo((int)Math.Round(bounds.X + spot.X * bounds.Width), (int)Math.Round(bounds.Y + spot.Y * bounds.Height)));
            }

            intents.Add(new KeyDown(pressedKey!.Value));
            dodging = pressedKey;
            dodgeHeldFor = TimeSpan.Zero;
        }

        dodgeHeld = pressed;

        if (dodging is not KeyCode key)
        {
            return false;
        }

        dodgeHeldFor += elapsed;

        if (dodgeHeldFor < ModeController.TapLength)
        {
            return true;
        }

        intents.Add(new KeyUp(key));
        dodging = null;
        cursorAt = null;
        return true;
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

    /// <summary>
    /// Where twin-stick aiming puts the cursor: the ground the right stick points
    /// at, further out the further it leans; with the stick let go, the ground
    /// ahead of the walk, or where it last pointed. An enemy close to that spot
    /// draws it, as soft aim does in third person.
    /// </summary>
    private (int X, int Y)? Aimed(StickPosition left, StickPosition right, TimeSpan elapsed, ScreenRect bounds)
    {
        double reach = 1;

        if (right.Magnitude > 0)
        {
            aimAngle = Math.Atan2(-right.Y, right.X);
            reach = 0.5 + right.Magnitude;
        }
        else if (left.Magnitude >= settings.WalkThreshold)
        {
            aimAngle = Math.Atan2(-left.Y, left.X);
        }

        ScreenSpot spot = GroundAim.Spot(Near, aimAngle, reach, Aspect());

        if (SoftAim is not null && SoftAim.Pick(Seen, spot.X, spot.Y, Aspect()) is SeenTarget soft && Glided(soft, elapsed) is (int X, int Y) onEnemy)
        {
            return onEnemy;
        }

        return ((int)Math.Round(bounds.X + spot.X * bounds.Width), (int)Math.Round(bounds.Y + spot.Y * bounds.Height));
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
    private void Look(double yaw, double zoom, List<InputIntent> intents, double tilt = 0)
    {
        if (Same(yaw, sentYaw) && Same(zoom, sentZoom) && Same(tilt, sentTilt))
        {
            return;
        }

        sentYaw = yaw;
        sentZoom = zoom;
        sentTilt = tilt;
        intents.Add(new GameCommandIntent(new LookCommand(yaw, zoom, tilt)));
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
        // The keys walk even while steering: the game only walks while one is down, and
        // the in-game part turns that walk to the stick's exact angle.
        keys.UnionWith(StickDirections.KeysFor(left, settings.WalkThreshold, settings.Movement));
        bool wheelOpen = TickWheel(snapshot, right, elapsed, keys);

        List<InputIntent> intents = differ.Update(keys, pointerButtons);
        Steer(left, elapsed, intents);
        TickLock(snapshot, right, elapsed, wheelOpen, intents);

        // While a dodge's key is down the cursor stays on its spot, or the skill would go elsewhere.
        if (TickDodge(snapshot, left, elapsed, intents))
        {
            return intents;
        }

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

            // As in action games the stick's up and down tilts the view; with the left
            // trigger held, it zooms instead. With no tilt speed set, it only zooms.
            if (settings.TiltDegreesPerSecond > 0 && snapshot.LeftTrigger < 0.5)
            {
                Look(yaw, 0, intents, right.Y * settings.TiltDegreesPerSecond);
                return intents;
            }

            Look(yaw, right.Y * settings.ZoomBlendPerSecond, intents);
            return intents;
        }

        // Leaving third person with the stick still over must not leave the camera turning.
        Look(0, 0, intents);

        if (TwinStick && InWorld && GameBounds is ScreenRect bounds)
        {
            Park(Aimed(left, right, elapsed, bounds), intents);
            return intents;
        }

        cursorAt = null;
        SoftAim?.Forget();
        PointerMove? move = cursor.Step(right, elapsed);

        if (move is not null)
        {
            intents.Add(move);
        }

        return intents;
    }
}
