using GDPilot.Core;
using GDPilot.Output;

namespace GDPilot.App;

/// <summary>
/// --targets: prints the enemies the in-game part reports, once a second, and
/// which one a press of lock-on would take, so a lock that will not come can
/// be told apart from an enemy the game does not offer.
/// </summary>
internal static class TargetsMode
{
    internal static int Run(GDPilotConfig config, TimeSpan? duration)
    {
        using GameLink link = new(new ReportedOccupancy(null), Console.WriteLine);
        TimeSpan length = TimeSpan.FromSeconds(10);

        if (duration is TimeSpan chosen)
        {
            length = chosen;
        }

        Console.WriteLine("Waiting for the in-game part; load it with --load-in-game if it is not running.");

        if (!SpinWait.SpinUntil(() => link.Connected, TimeSpan.FromSeconds(5)))
        {
            Console.Error.WriteLine("The in-game part did not connect within five seconds.");
            return 1;
        }

        DateTime end = DateTime.UtcNow + length;

        while (DateTime.UtcNow < end)
        {
            Thread.Sleep(1000);
            Print(link.Targets, config.LockOn());
        }

        return 0;
    }

    private static void Print(IReadOnlyList<SeenTarget> targets, LockOn lockOn)
    {
        Console.WriteLine($"{targets.Count} foes in reach:");

        foreach (SeenTarget target in targets.OrderBy(target => target.Distance))
        {
            string where = "out of view or behind a wall";

            if (target.OnScreen)
            {
                where = $"in view at {target.X:F2}, {target.Y:F2}";
            }

            Console.WriteLine($"  {target.Id}: {target.Distance:F1} away, {target.Health}% life, {where}");
        }

        if (lockOn.Toggle(targets, 0.5, 0.5) == LockChange.Locked)
        {
            Console.WriteLine($"  A press would lock onto {lockOn.Target!.Id}.");
            return;
        }

        Console.WriteLine("  A press would find nothing to lock onto.");
    }
}
