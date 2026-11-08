using GDPilot.Core;
using GDPilot.Output;

namespace GDPilot.App;

internal static class Program
{
    private const int FaultExit = 70;

    private static int Main(string[] args)
    {
        int code = Guarded(args);

        if (ClosingWindow.ShouldWait(code, ConsoleOwners.Count(), Console.IsInputRedirected || Console.IsErrorRedirected))
        {
            Console.Error.WriteLine("Press Enter to close this window.");
            Console.ReadLine();
        }

        return code;
    }

    /// <summary>
    /// A fault nothing else caught stops here rather than leaving the program:
    /// .NET can end a process on an unhandled exception without running any cleanup
    /// on the way, and the cleanup is what lets go of the keys GDPilot holds down.
    /// </summary>
    private static int Guarded(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Exception fault)
        {
            Record(fault);
            return FaultExit;
        }
    }

    private static void Record(Exception fault)
    {
        string log = Path.Combine(ConfigFile.OwnFolder, "crash.log");
        Console.Error.WriteLine($"GDPilot stopped on a fault: {fault.Message}");

        try
        {
            Directory.CreateDirectory(ConfigFile.OwnFolder);
            File.AppendAllText(log, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {fault}{Environment.NewLine}{Environment.NewLine}");
            Console.Error.WriteLine($"The details are in {log}.");
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine(fault);
        }
    }

    private static int Run(string[] args)
    {
        // Before anything reads or sends a screen coordinate.
        DpiAwareness.EnablePerMonitor();

        if (CommandLine.Unknown(args) is string unknown)
        {
            Console.Error.WriteLine($"GDPilot does not know {unknown}. It knows: {string.Join(", ", CommandLine.Options)}.");
            return 2;
        }

        bool dryRun = args.Contains("--dry-run");
        TimeSpan? duration = null;
        (double? seconds, string? secondsProblem) = CommandLine.Number(args, "--seconds");

        if (secondsProblem is not null)
        {
            Console.Error.WriteLine(secondsProblem);
            return 2;
        }

        if (seconds is double given)
        {
            duration = TimeSpan.FromSeconds(given);
        }

        int detectAt = Array.IndexOf(args, "--detect");

        if (detectAt >= 0 && detectAt + 5 < args.Length)
        {
            return DetectMode.Run(args.Skip(detectAt + 1).Take(5).ToArray());
        }

        GDPilotConfig config;

        try
        {
            config = ConfigFile.Load(Console.Out);
        }
        catch (Exception problem) when (problem is FormatException or System.Text.Json.JsonException or IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"The settings file could not be read: {problem.Message}");
            return 2;
        }

        int captureAt = Array.IndexOf(args, "--capture");

        if (captureAt >= 0 && captureAt + 1 < args.Length)
        {
            string directory = "captures";
            int toAt = Array.IndexOf(args, "--to");

            if (toAt >= 0 && toAt + 1 < args.Length)
            {
                directory = args[toAt + 1];
            }

            return CaptureMode.Run(config, args[captureAt + 1], directory);
        }

        if (args.Contains("--rumble-test"))
        {
            return RumbleTestMode.Run();
        }

        if (args.Contains("--targets"))
        {
            return TargetsMode.Run(config, duration);
        }

        if (args.Contains("--install-loader"))
        {
            return LoaderMode.Install(config);
        }

        if (args.Contains("--remove-loader"))
        {
            return LoaderMode.Remove(config);
        }

        if (args.Contains("--setup"))
        {
            return SetupMode.Run(config);
        }

        if (args.Contains("--load-in-game"))
        {
            return LoadInGameMode.Run(config);
        }

        int checkAt = Array.IndexOf(args, "--check-game");

        if (checkAt >= 0)
        {
            string? folder = null;

            if (checkAt + 1 < args.Length)
            {
                folder = args[checkAt + 1];
            }

            return CheckGameMode.Run(config, folder);
        }

        int occupancyAt = Array.IndexOf(args, "--occupancy");

        if (occupancyAt >= 0 && occupancyAt + 1 < args.Length)
        {
            return OccupancyMode.Run(config, args[occupancyAt + 1]);
        }

        int spotsAt = Array.IndexOf(args, "--spots");

        if (spotsAt >= 0 && spotsAt + 1 < args.Length)
        {
            return SpotsMode.Run(config, args[spotsAt + 1]);
        }

        int calibrateAt = Array.IndexOf(args, "--calibrate");

        if (calibrateAt >= 0 && calibrateAt + 1 < args.Length)
        {
            return CalibrateMode.Run(config, args[calibrateAt + 1], Numbers(args, "--at", 4), Counts(args));
        }

        if (ConfigFile.FirstRun)
        {
            SetupMode.Run(config);
        }

        Console.WriteLine($"GDPilot. Kill switch: {config.KillSwitch}. Press Ctrl+C to quit.");

        if (dryRun)
        {
            Console.WriteLine("Dry run: nothing is injected; intents are printed instead.");
        }

        GamepadTrace? replay = null;
        int replayAt = Array.IndexOf(args, "--replay");

        if (replayAt >= 0 && replayAt + 1 < args.Length)
        {
            try
            {
                replay = GamepadTrace.Parse(File.ReadAllText(args[replayAt + 1]));
            }
            catch (Exception problem) when (problem is FormatException or System.Text.Json.JsonException or IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Could not read the trace {args[replayAt + 1]}: {problem.Message}");
                return 2;
            }
        }

        // Two copies would each read the pad and press keys, doubling everything; a dry run presses nothing, so it may run beside one.
        using Mutex running = new(false, RunningName);

        if (!dryRun && !Owns(running))
        {
            Console.Error.WriteLine("GDPilot is already running; its icon is beside the clock. Quit that one first.");
            return 3;
        }

        return Host.Run(config, dryRun, duration, replay);
    }

    /// <summary>One GDPilot at a time for each person signed in to Windows.</summary>
    internal const string RunningName = @"Local\GDPilot.Running";

    private static bool Owns(Mutex running)
    {
        try
        {
            return running.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            // A copy that ended without letting go has gone all the same.
            return true;
        }
    }

    /// <summary>The given number of whole numbers after a flag, or null when the flag is absent or they do not read, which is said.</summary>
    private static int[]? Numbers(string[] args, string flag, int count)
    {
        (int[]? values, string? problem) = CommandLine.Whole(args, flag, count);

        if (problem is not null)
        {
            Console.Error.WriteLine(problem);
        }

        return values;
    }

    private static (int Columns, int Rows)? Counts(string[] args)
    {
        int[]? counts = Numbers(args, "--counts", 2);

        if (counts is null)
        {
            return null;
        }

        return (counts[0], counts[1]);
    }
}
