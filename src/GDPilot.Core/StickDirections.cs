namespace GDPilot.Core;

/// <summary>The four keys a stick drives, for example W, S, A and D.</summary>
public sealed record DirectionKeys(KeyCode Up, KeyCode Down, KeyCode Left, KeyCode Right)
{
    public static readonly DirectionKeys Wasd = new(KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D);
}

public static class StickDirections
{
    /// <summary>
    /// Eight-way digital movement. Each direction owns a 45 degree sector
    /// centred on it, and diagonals hold both of their keys. Below the
    /// threshold nothing is held, since keyboard movement has no half speed.
    /// </summary>
    public static IReadOnlySet<KeyCode> KeysFor(StickPosition stick, double threshold, DirectionKeys keys)
    {
        HashSet<KeyCode> held = new();

        if (stick.Magnitude < threshold)
        {
            return held;
        }

        double degrees = Math.Atan2(stick.Y, stick.X) * 180 / Math.PI;
        int sector = (int)Math.Round(degrees / 45, MidpointRounding.AwayFromZero);

        // Sectors run anticlockwise from 0 at right: 2 is up, 4 left, 6 down.
        sector = (sector % 8 + 8) % 8;

        if (sector is 7 or 0 or 1)
        {
            held.Add(keys.Right);
        }

        if (sector is 1 or 2 or 3)
        {
            held.Add(keys.Up);
        }

        if (sector is 3 or 4 or 5)
        {
            held.Add(keys.Left);
        }

        if (sector is 5 or 6 or 7)
        {
            held.Add(keys.Down);
        }

        return held;
    }
}
