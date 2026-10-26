using System.Diagnostics;
using GDPilot.Output;

namespace GDPilot.Integration.Tests;

/// <summary>
/// The loader, used on a harmless stand-in: a hidden ping process made to load
/// a system DLL it would not otherwise need. No game is involved.
/// </summary>
public sealed class GameLoaderTests
{
    [Fact]
    public void AnotherProcessIsMadeToLoadTheDll()
    {
        using Process host = Process.Start(new ProcessStartInfo("ping.exe", "-n 20 127.0.0.1") { CreateNoWindow = true, UseShellExecute = false })!;

        try
        {
            Thread.Sleep(300);
            Assert.False(GameLoader.IsLoaded(host, "dbghelp.dll"));

            string? problem = GameLoader.Load(host, Path.Combine(Environment.SystemDirectory, "dbghelp.dll"));

            Assert.Null(problem);
            Assert.True(GameLoader.IsLoaded(host, "dbghelp.dll"));
        }
        finally
        {
            host.Kill();
        }
    }

    [Fact]
    public void AMissingDllIsRefusedBeforeTouchingTheProcess()
    {
        Assert.Contains("does not exist", GameLoader.Load(Process.GetCurrentProcess(), Path.Combine("nowhere", "missing.dll")));
    }
}
