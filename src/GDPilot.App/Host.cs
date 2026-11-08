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
        using TrayIcon? tray = StartTray(replay, suspension, stop);

        // Parsed once up front, since the loop swaps tables on every mode change.
        Dictionary<Mode, ButtonLayers> layers = new();

        foreach (Mode mode in Enum.GetValues<Mode>())
        {
            layers[mode] = config.ButtonsFor(mode.ToString());
        }

        // Section 11: grids are worked out for the live display, and again whenever it changes.
        GridReadiness readiness = new(false, "The game is not running yet, so grid navigation is off.", new Dictionary<string, GridSpec>());
        DisplayWatch displayWatch = new();
        InGameAutoLoad autoLoad = new();

        if (replay is null)
        {
            InGamePart.PointLoaderAtCurrent();
        }
        string? character = null;
        SettingsWatch? settingsWatch = WatchSettings();
        string? settingsProblem = null;
        LiveDisplay? display = GameDisplay.Read(config);

        if (displayWatch.Changed(display))
        {
            readiness = GridReadiness.Check(config, display!.Value);
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

        ScreenSpots spots = new();
        GridNavigator navigator = new(readiness, jumping, spots) { Carrying = () => link.Carrying, ReportedTab = mode => occupancy.TabOf(mode.ToString()), ReportedTabs = mode => occupancy.TabsOf(mode.ToString()) };
        ModeController modes = new(config.PanelKeys(), mode => layers[mode], navigator);
        ConversationWatch conversation = new(() => ConversationShowing(readiness, spots));
        Mapper mapper = new(config.Mapping(), layers[Mode.Combat]) { Wheel = config.Wheel(), Lock = config.LockOn(), SoftAim = config.SoftAim(), TwinStick = config.Input.TwinStick, InteractReach = config.Input.InteractReach, HotbarSlot = key => HotbarSlot(readiness, key) };

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
        spots.Card = () => overlay?.CardBounds;

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
            PadStyle padStyle = PadGlyphs.Chosen(config.Input.PadStyle) ?? PadGlyphs.StyleOf(sdl?.Kind ?? "", sdl?.Name);
            Console.WriteLine($"Buttons read as on a {padStyle} pad.");
            PadWatch padWatch = new();
            CombatRumble fight = new();
            DropRumble drops = new();
            LegendFold fold = new();
            IReadOnlyList<Picture?>? wheelPictures = null;
            ScreenRect? gameBounds = null;
            IntPtr gameHandle = IntPtr.Zero;
            TimeSpan lastDrawn = TimeSpan.Zero;
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
                    gameHandle = GameWindow.Handle(config.GameProcess);
                    mapper.GameBounds = gameBounds;
                    display = GameDisplay.Read(gameBounds);

                    if (displayWatch.Changed(display))
                    {
                        readiness = GridReadiness.Check(config, display!.Value);
                        navigator.Use(readiness);
                        Console.WriteLine($"Grids: {readiness.Message}");
                    }

                    if (sdl is not null)
                    {
                        padStyle = FollowPad(sdl, padWatch, mapper, config, padStyle);
                    }

                    if (settingsWatch?.Changed(now) is GDPilotConfig saved)
                    {
                        config = saved;

                        foreach (Mode mode in Enum.GetValues<Mode>())
                        {
                            layers[mode] = config.ButtonsFor(mode.ToString(), character);
                        }

                        mapper.Use(config.Mapping());
                        mapper.Wheel = config.Wheel();
                        mapper.Lock = config.LockOn();
                        mapper.SoftAim = config.SoftAim();
                        mapper.TwinStick = config.Input.TwinStick;
                        mapper.InteractReach = config.Input.InteractReach;

                        if (display is LiveDisplay shown)
                        {
                            readiness = GridReadiness.Check(config, shown);
                            navigator.Use(readiness);
                        }

                        settingsProblem = null;
                        Console.WriteLine("Settings saved, and taken at once. The kill switch, the panel keys and the poll rate wait for a restart.");
                    }
                    else if (settingsWatch?.Problem is string problem && problem != settingsProblem)
                    {
                        settingsProblem = problem;
                        Console.WriteLine($"Settings saved but not taken, so the last good ones stand: {problem}");
                        tray?.Say($"Settings not taken, the last good ones stand: {problem}");
                    }

                    if (link.Character is string playing && playing != character)
                    {
                        character = playing;

                        foreach (Mode mode in Enum.GetValues<Mode>())
                        {
                            layers[mode] = config.ButtonsFor(mode.ToString(), character);
                        }

                        Console.WriteLine($"Playing {character}.");
                    }

                    if (config.Input.LoadInGame && !dryRun && replay is null)
                    {
                        LoadInGameWhenDue(config, autoLoad, gameHandle, link.Connected, now, tray);
                    }
                }

                // With the in-game part connected, the game says which panels are open; without it, presses are tracked.
                if (link.Panels is PanelsReport panels)
                {
                    modes.Follow(conversation.Filter(panels.Open, now - last));
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

                if (sdl is not null && config.Input.Rumble && drops.Update(link.Things) is Rumble drop)
                {
                    Console.WriteLine("A fine drop has come into reach.");
                    sdl.Rumble(drop.Heavy, drop.Light, drop.Length);
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

                    OverlayState state = OverlayState.Build(loop.Mode, suspension.Reasons, layers[loop.Mode], modes.GridFellBack, link.ThirdPerson, locked, mapper.Steering, config.Input.TiltSpeed > 0, padStyle, sdl is not null && !sdl.IsConnected) with
                    {
                        Wheel = view,
                        Marker = LockMarker.For(locked, mapper.LockPoint),
                        Prompt = OverlayState.PromptFor(mapper.Offered, layers[loop.Mode], padStyle),
                    };
                    state = state with { Folded = fold.Folded(state, now - lastDrawn) };
                    lastDrawn = now;
                    overlay.Update(state.Floating(), gameBounds, config.Overlay.Corner, gameHandle, display);
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

    private static (int X, int Y)? HotbarSlot(GridReadiness readiness, KeyCode key)
    {
        if (readiness.Grids.TryGetValue("Hotbar", out GridSpec? hotbar) && RadialMenu.HotbarColumn(key) is int column)
        {
            return hotbar.Centre(new GridCell(column, 0));
        }

        return null;
    }

    private static bool ConversationShowing(GridReadiness readiness, ScreenSpots spots)
    {
        if (readiness.Spots.TryGetValue(nameof(Mode.Dialog), out SpotArea? area))
        {
            return spots.ConversationShowing(area);
        }

        return true;
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

    /// <summary>Off the loop's own thread, since loading takes as long as the game needs and the pad must not wait for it.</summary>
    private static void LoadInGameWhenDue(GDPilotConfig config, InGameAutoLoad autoLoad, IntPtr gameHandle, bool connected, TimeSpan now, TrayIcon? tray)
    {
        int? process = null;

        if (gameHandle != IntPtr.Zero)
        {
            process = GameWindow.ProcessId(gameHandle);
        }

        if (autoLoad.Due(process, connected, now))
        {
            Task.Run(() =>
            {
                if (LoadInGameMode.Run(config) != 0)
                {
                    tray?.Say("The camera and lock-on are off, as the in-game part could not be loaded. The reason is in gdpilot.log, under Log folder in this icon's menu.");
                }
            });
        }
    }

    /// <summary>
    /// A pad pulled out lets go of everything by reading at rest. One plugged back
    /// keeps its calibration if it is the same pad; another starts from the middle,
    /// and has its own button names.
    /// </summary>
    private static PadStyle FollowPad(SdlGamepadSource sdl, PadWatch padWatch, Mapper mapper, GDPilotConfig config, PadStyle style)
    {
        PadChange change = padWatch.Changed(sdl.IsConnected, sdl.Name);

        if (change == PadChange.Lost)
        {
            Console.WriteLine("Pad disconnected: everything it held is let go. Plug it back in to carry on.");
            return style;
        }

        if (change != PadChange.Found)
        {
            return style;
        }

        if (!padWatch.SameAsBefore(sdl.Name))
        {
            mapper.LeftCentre = StickPosition.Centre;
            mapper.RightCentre = StickPosition.Centre;
        }

        PadStyle found = PadGlyphs.Chosen(config.Input.PadStyle) ?? PadGlyphs.StyleOf(sdl.Kind, sdl.Name);
        Console.WriteLine($"Pad connected: {sdl.Name}, read as a {found} pad.");
        return found;
    }

    /// <summary>The person's own settings file, watched for saves; a run on the shipped settings alone has nothing to watch.</summary>
    private static SettingsWatch? WatchSettings()
    {
        if (ConfigFile.LoadedFrom is not string path)
        {
            return null;
        }

        return new SettingsWatch(() => Written(path), () => ConfigFile.Read(path));
    }

    private static DateTime? Written(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        return File.GetLastWriteTimeUtc(path);
    }

    /// <summary>A replay is a test with an end of its own, so it leaves nothing beside the clock.</summary>
    private static TrayIcon? StartTray(GamepadTrace? replay, Suspension suspension, CancellationTokenSource stop)
    {
        if (replay is not null || ConfigFile.LoadedFrom is not string settings)
        {
            return null;
        }

        return new TrayIcon(() => Toggle(suspension), () => (suspension.Reasons & SuspendReason.KillSwitch) != 0, stop.Cancel, settings);
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
