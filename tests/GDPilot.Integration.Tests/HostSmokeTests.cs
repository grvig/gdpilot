using System.Diagnostics;

namespace GDPilot.Integration.Tests;

/// <summary>
/// Starts the real host in a dry run for a second, with or without a pad, and
/// checks it comes up, runs and shuts down cleanly. Nothing is injected.
/// </summary>
public sealed class HostSmokeTests
{
    [Fact]
    public void TheHostStartsRunsAndStopsInADryRun()
    {
        // Settings of its own in the folder it runs from, so the run is never a first run that writes into the person's own folder.
        string folder = Path.Combine(Path.GetTempPath(), "GDPilotSmoke-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "config.example.json"), Path.Combine(folder, "config.json"));
        ProcessStartInfo start = new(Path.Combine(AppContext.BaseDirectory, "GDPilot.exe"), "--dry-run --seconds 1");
        start.WorkingDirectory = folder;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.UseShellExecute = false;

        using Process host = Process.Start(start)!;
        string output = host.StandardOutput.ReadToEnd();
        string errors = host.StandardError.ReadToEnd();
        Assert.True(host.WaitForExit(15000), "The host did not stop on its own.");

        Assert.True(host.ExitCode == 0, $"Exit code {host.ExitCode}. Output:\n{output}\nErrors:\n{errors}");
        Assert.Contains("Dry run", output);
        Assert.Contains("Stopped. Everything held has been released.", output);
        Assert.DoesNotContain("First run", output);
        Directory.Delete(folder, true);
    }

    [Fact]
    public void ASecondCopySaysOneIsRunningAndStopsBeforeTouchingAnything()
    {
        using Mutex first = new(true, @"Local\GDPilot.Running", out bool created);
        Assert.True(created, "A GDPilot is running on this machine; quit it to run this test.");
        string folder = Path.Combine(Path.GetTempPath(), "GDPilotSecond-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "config.example.json"), Path.Combine(folder, "config.json"));
        ProcessStartInfo start = new(Path.Combine(AppContext.BaseDirectory, "GDPilot.exe"), "--seconds 5");
        start.WorkingDirectory = folder;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.UseShellExecute = false;

        using Process second = Process.Start(start)!;
        string errors = second.StandardError.ReadToEnd();
        Assert.True(second.WaitForExit(15000), "The second copy did not stop.");

        Assert.Equal(3, second.ExitCode);
        Assert.Contains("already running", errors);
        Directory.Delete(folder, true);
    }
}
