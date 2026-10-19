using System.Diagnostics;
using GDPilot.Core;

namespace GDPilot.App;

/// <summary>The dry-run sink: prints what would have been injected, with a timestamp.</summary>
internal sealed class ConsoleSink : IInputSink
{
    private readonly Stopwatch clock = Stopwatch.StartNew();

    public void Send(InputIntent intent)
    {
        Console.WriteLine($"{clock.Elapsed.TotalSeconds,8:F3}s  {intent}");
    }
}
