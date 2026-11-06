namespace GDPilot.Core;

/// <summary>
/// A shiver of the pad when something worth stopping for comes into reach. The
/// game's own numbers: 1 magic, 2 rare, 3 epic and 4 legendary, as its function
/// naming them has them, with 2 and 3 confirmed on dropped items on 2026-11-05;
/// materials read 0, and the purple oddities in the bags that are not gear 9.
/// Rares fall too often to be news, so it starts at epic, and a legendary is
/// felt more strongly. Each thing is felt once, and what is already lying around
/// as the report first arrives is not news at all.
/// </summary>
public sealed class DropRumble
{
    private const int Epic = 3;
    private const int NotGear = 9;

    private readonly HashSet<uint> felt = new();
    private IReadOnlyList<NearbyThing>? lastReport;

    /// <param name="things">The latest report of things nearby; only a new one is read.</param>
    public Rumble? Update(IReadOnlyList<NearbyThing> things)
    {
        if (ReferenceEquals(things, lastReport))
        {
            return null;
        }

        bool first = lastReport is null;
        lastReport = things;
        Rumble? rumble = null;

        foreach (NearbyThing thing in things)
        {
            if (thing.Kind != ThingKind.Loot || thing.Rarity < Epic || thing.Rarity >= NotGear || !felt.Add(thing.Id) || first)
            {
                continue;
            }

            if (thing.Rarity > Epic)
            {
                return new Rumble(0.7, 0.7, TimeSpan.FromMilliseconds(320));
            }

            rumble = new Rumble(0.35, 0.5, TimeSpan.FromMilliseconds(180));
        }

        return rumble;
    }
}
