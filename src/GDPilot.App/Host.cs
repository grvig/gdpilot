using System.Diagnostics;
using System.Runtime.InteropServices;
using GDPilot.Core;
using GDPilot.Input;
using GDPilot.Output;
using GDPilot.Overlay;

namespace GDPilot.App;

/// <summary>Wires the real pad, the safety layer and the control loop together, then runs the loop.</summary>
internal static class Host
{
    private static readonly TimeSpan StallThreshold = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan WatchdogPoll = TimeSpan.FromMilliseconds(50);

    internal static int Run(GDPilotConfig config, bool dryRun, TimeSpan? duration)
    {
        IInputSink output = new SendInputSink();
        Func<bool> gameInFront = () => ForegroundProbe.IsInFront(config.GameProcess);

        if (dryRun)
        {
            output = new ConsoleSink();

            // A dry run should show output with the game closed.
            gameInFront = () => true;
        }

        GuardedSink guarded = new(output);
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
        using SdlGamepadSource pad = new();

        // Parsed once up front, since the loop swaps tables on every mode change.
        Dictionary<Mode, ButtonLayers> layers = new();

        foreach (Mode mode in Enum.GetValues<Mode>())
        {
            layers[mode] = config.ButtonsFor(mode.ToString());
        }

        ModeController modes = new(config.PanelKeys(), mode => layers[mode]);
        Mapper mapper = new(config.Mapping(), layers[Mode.Combat]);
        Calibrate(pad, mapper);
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

            while (!stop.IsCancellationRequested && (duration is null || clock.Elapsed < duration))
            {
                TimeSpan now = clock.Elapsed;
                loop.Tick(now - last);
                last = now;
                ticks++;

                if (loop.Mode != shownMode)
                {
                    shownMode = loop.Mode;
                    Console.WriteLine($"Mode: {shownMode}");
                }

                if (overlay is not null && ticks % overlayInterval == 0)
                {
                    OverlayState state = OverlayState.Build(loop.Mode, suspension.Reasons, layers[loop.Mode]);
                    overlay.Update(state, GameWindow.ClientBounds(config.GameProcess), config.Overlay.Corner);
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
