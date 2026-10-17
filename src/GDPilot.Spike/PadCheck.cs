using System.Diagnostics;
using GDPilot.Core;
using GDPilot.Input;

namespace GDPilot.Spike;

/// <summary>Reads the real pad at rest and checks its drift against the default deadzones.</summary>
internal static class PadCheck
{
    // Section 7's example configuration.
    private const double LeftDeadzone = 0.22;
    private const double RightDeadzone = 0.18;

    internal static int Run()
    {
        using SdlGamepadSource source = new();
        source.Read();

        if (!source.IsConnected)
        {
            Console.WriteLine("No pad connected.");
            return 1;
        }

        Console.WriteLine($"Pad: {source.Name}. Sampling for half a second; leave the sticks alone.");
        List<StickPosition> left = new();
        List<StickPosition> right = new();
        Stopwatch clock = Stopwatch.StartNew();

        while (clock.ElapsedMilliseconds < 500)
        {
            GamepadSnapshot snapshot = source.Read();
            left.Add(snapshot.LeftStick);
            right.Add(snapshot.RightStick);
            Thread.Sleep(4);
        }

        Report("Left stick", left, LeftDeadzone);
        Report("Right stick", right, RightDeadzone);
        return 0;
    }

    private static void Report(string name, List<StickPosition> samples, double deadzone)
    {
        if (!StickCalibration.TryFindCentre(samples, out StickPosition centre))
        {
            Console.WriteLine($"{name}: moving or held while sampled, so it could not be calibrated.");
            return;
        }

        double jitter = samples.Max(sample => new StickPosition(sample.X - centre.X, sample.Y - centre.Y).Magnitude);
        Console.WriteLine($"{name}: rests at ({centre.X:F4}, {centre.Y:F4}), offset {centre.Magnitude:F4}, jitter {jitter:F4}, {samples.Count} samples.");

        // Calibration removes the offset, so only jitter has to fit inside the
        // deadzone. Without calibration the offset has to fit as well.
        Console.WriteLine($"  Deadzone {deadzone}: {Verdict(jitter < deadzone)} once calibrated, {Verdict(centre.Magnitude + jitter < deadzone)} without.");
    }

    private static string Verdict(bool fits)
    {
        if (fits)
        {
            return "fits";
        }

        return "DOES NOT FIT";
    }
}
