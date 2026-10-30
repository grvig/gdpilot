using System.Diagnostics;
using System.Runtime.InteropServices;
using GDPilot.Core;
using GDPilot.Input;
using GDPilot.Output;
using GDPilot.Overlay;
using GDPilot.Vision;

namespace GDPilot.App;

/// <summary>Wires the real pad, the safety layer and the control loop together, then runs the loop.</summary>
internal static class Host
{
    private static readonly TimeSpan StallThreshold = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan WatchdogPoll = TimeSpan.FromMilliseconds(50);

    /// <param name="replay">A recorded pad performance to play instead of reading the real pad, or null.</param>
    internal static int Run(GDPilotConfig config, bool dryRun, TimeSpan? duration, GamepadTrace? replay)
    {
        IInputSink output = new SendInputSink();
        Func<bool> gameInFront = () => ForegroundProbe.IsInFront(config.GameProcess);

        if (dryRun)
        {
            output = new ConsoleSink();

            // A dry run should show output with the game closed.
            gameInFront = () => true;
        }

        CommandRouter router = new(output);
        GuardedSink guarded = new(router);
        Suspension suspension = new(guarded);

        // A synthetic key left down outlives this process, so every way out releases first.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => guarded.ReleaseAll();
        AppDomain.CurrentDomain.UnhandledException += (_, _) => guarded.ReleaseAll();

        using CancellationTokenSource stop = new();
        Console.CancelKeyPress += (_, press) =>
        {
            press.Cancel = true;
            stop.Cancel();
        };

        Stopwatch clock = Stopwatch.StartNew();
        StallDetector stall = new(StallThreshold, () => clock.Elapsed);
        using Watchdog watchdog = new(stall, WatchdogPoll, guarded.ReleaseAll);
        using KillSwitchHook killSwitch = new(KeyChord.Parse(config.KillSwitch), () => Toggle(suspension));

        // Parsed once up front, since the loop swaps tables on every mode change.
        Dictionary<Mode, ButtonLayers> layers = new();

        foreach (Mode mode in Enum.GetValues<Mode>())
        {
            layers[mode] = config.ButtonsFor(mode.ToString());
        }

        // Section 11: grids are checked against the live display once, at startup,
        // and refused with a reason rather than clicking the wrong pixels.
        GridReadiness readiness = new(false, "The game was not running when GDPilot started, so grid navigation is off.", new Dictionary<string, GridSpec>());

        if (GameDisplay.Read(config) is LiveDisplay live)
        {
            readiness = GridReadiness.Check(config, live);
        }

        Console.WriteLine($"Grids: {readiness.Message}");
        IOccupancySource? screen = null;

        if (config.Input.JumpToItems)
        {
            screen = new ScreenOccupancy();
        }

        // The in-game part, when it runs, reports exact item layouts; until then the screen is read.
        ReportedOccupancy occupancy = new(screen);
        using GameLink link = new(occupancy, Console.WriteLine);

        if (!dryRun)
        {
            router.Link = link;
        }
        IOccupancySource? jumping = null;

        if (config.Input.JumpToItems)
        {
            jumping = occupancy;
        }

        ModeController modes = new(config.PanelKeys(), mode => layers[mode], new GridNavigator(readiness, jumping));
        Mapper mapper = new(config.Mapping(), layers[Mode.Combat]) { Wheel = config.Wheel(), Lock = config.LockOn(), SoftAim = config.SoftAim(), TwinStick = config.Input.TwinStick };

        // A replay is timed from the first tick, so startup does not eat into it.
        Stopwatch replayClock = new();
        using SdlGamepadSource? sdl = OpenPad(replay);
        IGamepadSource pad;

        if (sdl is not null)
        {
            pad = sdl;
            Calibrate(sdl, mapper);
        }
        else
        {
            pad = new ReplayGamepadSource(replay!, () => replayClock.Elapsed);
            duration = TimeSpan.FromMilliseconds(replay!.Entries[^1].At + 500);
            Console.WriteLine($"Replaying {replay.Entries.Count} pad states over {duration.Value.TotalSeconds:F1} s instead of reading the pad.");
        }

        ControlLoop loop = new(pad, modes, mapper, guarded, stall, new FocusWatcher(gameInFront, suspension), suspension);
        using OverlayHost? overlay = StartOverlay(config);

        // About ten redraws a second: enough to feel immediate, cheap enough to ignore.
        long overlayInterval = Math.Max(1, config.Input.PollHz / 10);
        Mode shownMode = loop.Mode;
        TimeSpan period = TimeSpan.FromSeconds(1.0 / config.Input.PollHz);

        // Windows sleeps in 15.6 ms steps by default, which would cap 250 Hz at about 64.
        TimeBeginPeriod(1);
        long ticks = 0;
        TimeSpan started = clock.Elapsed;

        try
        {
            TimeSpan last = clock.Elapsed;
            bool wasSuspended = false;
            bool wheelShown = false;
            bool markerShown = false;
            RumblePolicy rumble = new();
            CombatRumble fight = new();
            IReadOnlyList<Picture?>? wheelPictures = null;
            ScreenRect? gameBounds = null;
            replayClock.Start();

            while (!stop.IsCancellationRequested && (duration is null || clock.Elapsed < duration))
            {
                TimeSpan now = clock.Elapsed;
                // Measured as third person begins, not every tick: finding the window is not free.
                if (link.ThirdPerson && !mapper.Looking)
                {
                    mapper.AimPoint = AimPoint(config);
                }

                mapper.Looking = link.ThirdPerson;
                mapper.Steering = config.Input.AnalogMovement && link.AnalogMovement;
                TargetsReport near = link.Near;
                mapper.Seen = near.Targets;
                mapper.Near = near;
                mapper.Things = link.Things;
                mapper.LockSpot = link.LockPoint;
                // A panel over the world, or the character lying dead, lets go of any lock.
                mapper.InWorld = loop.Mode == Mode.Combat && link.Health != 0;

                // Lock-on places the cursor by the game window, so its bounds are kept fresh whether or not the overlay draws.
                if (ticks % overlayInterval == 0)
                {
                    gameBounds = GameWindow.ClientBounds(config.GameProcess);
                    mapper.GameBounds = gameBounds;
                }

                // With the in-game part connected, the game says which panels are open; without it, presses are tracked.
                if (link.Panels is PanelsReport panels)
                {
                    modes.Follow(panels.Open);
                }
                StopTurningWhenSuspended(suspension, link, ref wasSuspended);
                loop.Tick(now - last);

                if (sdl is not null && config.Input.Rumble && rumble.Tick(link.Health, now - last) is Rumble buzz)
                {
                    sdl.Rumble(buzz.Heavy, buzz.Light, buzz.Length);
                }

                if (sdl is not null && config.Input.Rumble && RumblePolicy.ForLock(mapper.LockChanged) is Rumble tick)
                {
                    sdl.Rumble(tick.Heavy, tick.Light, tick.Length);
                }

                if (sdl is not null && config.Input.Rumble && fight.Update(mapper.Seen, mapper.Lock?.Target?.Id, now - last) is Rumble blow)
                {
                    sdl.Rumble(blow.Heavy, blow.Light, blow.Length);
                }

                last = now;
                ticks++;

                if (loop.Mode != shownMode)
                {
                    shownMode = loop.Mode;
                    Console.WriteLine($"Mode: {shownMode}");
                }

                // The wheel follows the stick and the marker its enemy, so while either is up
                // the overlay is redrawn about fifty times a second.
                bool regular = ticks % overlayInterval == 0;
                bool wheelOpen = mapper.Wheel is not null && mapper.Wheel.Open;
                SeenTarget? locked = mapper.Lock?.Target;
                bool moving = wheelOpen || locked is not null;

                // A locked marker keeps up with the per-frame lock point: about a hundred redraws a second.
                int movingEvery = 5;

                if (locked is not null)
                {
                    movingEvery = 2;
                }

                if (overlay is not null && (regular || (moving && ticks % movingEvery == 0) || wheelOpen != wheelShown || (locked is not null) != markerShown))
                {
                    // The icons are copied once as the wheel opens; the hotbar does not change while it is held.
                    if (wheelOpen && !wheelShown)
                    {
                        wheelPictures = HotbarPictures(readiness, mapper.Wheel!);
                    }

                    WheelView? view = mapper.Wheel?.View();

                    if (view is not null)
                    {
                        view = view with { Pictures = wheelPictures };
                    }

                    OverlayState state = OverlayState.Build(loop.Mode, suspension.Reasons, layers[loop.Mode], modes.GridFellBack, link.ThirdPerson, locked, mapper.Steering, config.Input.TiltSpeed > 0) with
                    {
                        Wheel = view,
                        Marker = LockMarker.For(locked, mapper.LockPoint),
                        Prompt = OverlayState.PromptFor(mapper.Offered, layers[loop.Mode]),
                    };
                    overlay.Update(state.Floating(), gameBounds, config.Overlay.Corner);
                    wheelShown = wheelOpen;
                    markerShown = locked is not null;
                }

                TimeSpan wait = period - (clock.Elapsed - now);

                if (wait > TimeSpan.Zero)
                {
                    Thread.Sleep(wait);
                }
            }
        }
        finally
        {
            guarded.ReleaseAll();
            TimeEndPeriod(1);
        }

        double seconds = (clock.Elapsed - started).TotalSeconds;
        Console.WriteLine($"Stopped. Everything held has been released. {ticks} ticks at {ticks / seconds:F0} Hz against {config.Input.PollHz} configured.");
        return 0;
    }

    /// <summary>
    /// Each wheel slot's icon, cut from one capture of the hotbar calibrated as the
    /// "Hotbar" grid, or null without that grid. A slot with nothing on it still
    /// gets its picture, which is the empty slot's dark square.
    /// </summary>
    private static IReadOnlyList<Picture?>? HotbarPictures(GridReadiness readiness, RadialMenu wheel)
    {
        if (!readiness.Ready || !readiness.Grids.TryGetValue("Hotbar", out GridSpec? hotbar))
        {
            return null;
        }

        int side = (int)(hotbar.PitchX * 0.8);
        int left = (int)Math.Round(hotbar.OriginX - side / 2.0);
        int top = (int)Math.Round(hotbar.OriginY - side / 2.0);
        int width = (int)Math.Ceiling(hotbar.PitchX * (hotbar.Columns - 1)) + side;
        CapturedFrame frame;

        try
        {
            frame = ScreenCapture.Region(left, top, width, side);
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        List<Picture?> pictures = new();

        foreach (KeyCode key in wheel.Slots)
        {
            if (RadialMenu.HotbarColumn(key) is not int column)
            {
                pictures.Add(null);
                continue;
            }

            (int x, int _) = hotbar.Centre(new GridCell(column, 0));
            pictures.Add(Cut(frame, x - side / 2 - left, side));
        }

        return pictures;
    }

    private static Picture Cut(CapturedFrame frame, int x, int side)
    {
        byte[] pixels = new byte[side * side * 4];

        for (int row = 0; row < side; row++)
        {
            Array.Copy(frame.Pixels, (row * frame.Width + x) * 4, pixels, row * side * 4, side * 4);
        }

        return new Picture(pixels, side, side);
    }

    /// <summary>The middle of the game window, at the configured height, or null when the game is not running.</summary>
    private static (int X, int Y)? AimPoint(GDPilotConfig config)
    {
        if (GameWindow.ClientBounds(config.GameProcess) is not ScreenRect game)
        {
            return null;
        }

        return (game.X + game.Width / 2, game.Y + (int)(game.Height * config.Input.AimHeight));
    }

    /// <summary>
    /// A suspension stops all input, including the commands that would stop the
    /// camera turning, so the camera is told to stop directly as one begins.
    /// </summary>
    private static void StopTurningWhenSuspended(Suspension suspension, GameLink link, ref bool wasSuspended)
    {
        bool suspended = suspension.Reasons != SuspendReason.None;

        if (suspended && !wasSuspended)
        {
            link.Send(new LookCommand(0, 0));
            link.Send(new LockCommand(0));
            link.Send(new MoveCommand(0, 0));
        }

        wasSuspended = suspended;
    }

    private static SdlGamepadSource? OpenPad(GamepadTrace? replay)
    {
        if (replay is not null)
        {
            return null;
        }

        return new SdlGamepadSource();
    }

    private static OverlayHost? StartOverlay(GDPilotConfig config)
    {
        if (!config.Overlay.Enabled)
        {
            return null;
        }

        return new OverlayHost();
    }

    private static void Calibrate(SdlGamepadSource pad, Mapper mapper)
    {
        pad.Read();

        if (!pad.IsConnected)
        {
            Console.WriteLine("No pad connected yet. It will be picked up when it is, uncalibrated.");
            return;
        }

        Console.WriteLine($"Pad: {pad.Name}. Calibrating; leave the sticks alone for half a second.");
        List<StickPosition> left = new();
        List<StickPosition> right = new();
        Stopwatch sampling = Stopwatch.StartNew();

        while (sampling.ElapsedMilliseconds < 500)
        {
            GamepadSnapshot snapshot = pad.Read();
            left.Add(snapshot.LeftStick);
            right.Add(snapshot.RightStick);
            Thread.Sleep(4);
        }

        if (StickCalibration.TryFindCentre(left, out StickPosition leftCentre) && StickCalibration.TryFindCentre(right, out StickPosition rightCentre))
        {
            mapper.LeftCentre = leftCentre;
            mapper.RightCentre = rightCentre;
            Console.WriteLine($"Calibrated. Left rests at ({leftCentre.X:F3}, {leftCentre.Y:F3}), right at ({rightCentre.X:F3}, {rightCentre.Y:F3}).");
            return;
        }

        Console.WriteLine("A stick moved during calibration, so true centre is assumed.");
    }

    private static void Toggle(Suspension suspension)
    {
        if ((suspension.Reasons & SuspendReason.KillSwitch) != 0)
        {
            suspension.Resume(SuspendReason.KillSwitch);
            Console.WriteLine("Kill switch released. Injection resumes.");
            return;
        }

        suspension.Suspend(SuspendReason.KillSwitch);
        Console.WriteLine("Kill switch. Injection stopped and everything held released.");
    }

    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    private static extern uint TimeBeginPeriod(uint milliseconds);

    [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    private static extern uint TimeEndPeriod(uint milliseconds);
}
