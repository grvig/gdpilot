using GDPilot.Core;
using GDPilot.Output;

namespace GDPilot.App;

internal static class Program
{
    private static int Main(string[] args)
    {
        // Before anything reads or sends a screen coordinate.
        DpiAwareness.EnablePerMonitor();

        bool dryRun = args.Contains("--dry-run");
        TimeSpan? duration = null;
        int secondsAt = Array.IndexOf(args, "--seconds");

        if (secondsAt >= 0 && secondsAt + 1 < args.Length)
        {
            duration = TimeSpan.FromSeconds(double.Parse(args[secondsAt + 1], System.Globalization.CultureInfo.InvariantCulture));
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
        catch (FormatException problem)
        {
            Console.Error.WriteLine($"The configuration is not valid: {problem.Message}");
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

        int calibrateAt = Array.IndexOf(args, "--calibrate");

        if (calibrateAt >= 0 && calibrateAt + 1 < args.Length)
        {
            return CalibrateMode.Run(config, args[calibrateAt + 1], Numbers(args, "--at", 4), Counts(args));
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
            replay = GamepadTrace.Parse(File.ReadAllText(args[replayAt + 1]));
        }

        return Host.Run(config, dryRun, duration, replay);
    }

    /// <summary>The given number of whole numbers after a flag, or null when the flag is absent or short.</summary>
    private static int[]? Numbers(string[] args, string flag, int count)
    {
        int at = Array.IndexOf(args, flag);

        if (at < 0 || at + count >= args.Length)
        {
            return null;
        }

        return args.Skip(at + 1).Take(count).Select(value => int.Parse(value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
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
