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

        int calibrateAt = Array.IndexOf(args, "--calibrate");

        if (calibrateAt >= 0 && calibrateAt + 1 < args.Length)
        {
            return CalibrateMode.Run(config, args[calibrateAt + 1]);
        }

        Console.WriteLine($"GDPilot. Kill switch: {config.KillSwitch}. Press Ctrl+C to quit.");

        if (dryRun)
        {
            Console.WriteLine("Dry run: nothing is injected; intents are printed instead.");
        }

        return Host.Run(config, dryRun, duration);
    }
}
