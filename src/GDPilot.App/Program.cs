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

        Console.WriteLine($"GDPilot. Kill switch: {config.KillSwitch}. Press Ctrl+C to quit.");

        if (dryRun)
        {
            Console.WriteLine("Dry run: nothing is injected; intents are printed instead.");
        }

        return Host.Run(config, dryRun, duration);
    }
}
