using GDPilot.Core;

namespace GDPilot.App;

/// <summary>
/// Prints the stops the screen reading finds for one menu right now, as --spots
/// Skills with the window open, one "x y" a line, the fixed places after them.
/// It shows at a glance why a press lands where it does.
/// </summary>
internal static class SpotsMode
{
    internal static int Run(GDPilotConfig config, string name)
    {
        if (!config.Profiles.TryGetValue(config.ActiveProfile, out ProfileConfig? profile) || !profile.Spots.TryGetValue(name, out SpotAreaConfig? entry) || entry.ToArea() is not SpotArea area)
        {
            Console.Error.WriteLine($"No spots named {name} in the active profile.");
            return 2;
        }

        ScreenSpots spots = new();
        List<ScreenPoint>? found = spots.Read(area);

        if (found is null)
        {
            Console.Error.WriteLine("The screen could not be captured.");
            return 1;
        }

        foreach (ScreenPoint spot in found.Concat(area.Fixed).Concat(spots.LitOf(area)))
        {
            Console.WriteLine($"{spot.X} {spot.Y}");
        }

        return 0;
    }
}
