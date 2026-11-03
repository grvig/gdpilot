namespace GDPilot.Core;

/// <summary>A place on screen the cursor can snap to, such as a skill or a star, in pixels.</summary>
public readonly record struct ScreenPoint(int X, int Y);

/// <summary>Finds the spots of a menu's area as they are on screen now, or null when the screen cannot be read.</summary>
public interface ISpotSource
{
    List<ScreenPoint>? Read(SpotArea area);
}

/// <summary>
/// Moves between spots that sit anywhere, not on a grid: the skill tree's
/// buttons, the stars of the devotion map. A press goes to the nearest spot
/// in its direction, where drifting sideways costs three times as much as
/// going straight, the same rule the bags use.
/// </summary>
public static class SpotNavigator
{
    /// <summary>
    /// How far a spot must lie ahead, against how far to the side, to count as
    /// in that direction at all: ahead by at least half its sideways distance.
    /// Spots found on screen sit a few pixels off their true centres, so two
    /// buttons in one row are never "below" one another.
    /// </summary>
    private const double Cone = 0.5;

    /// <summary>Less than this ahead is the same row or column, whatever the cone says.</summary>
    private const int Level = 6;

    /// <summary>The spot to start on: the one nearest the top-left corner, or null when there are none.</summary>
    public static ScreenPoint? First(IReadOnlyList<ScreenPoint> spots)
    {
        ScreenPoint? best = null;

        foreach (ScreenPoint spot in spots)
        {
            if (best is null || spot.X + spot.Y < best.Value.X + best.Value.Y)
            {
                best = spot;
            }
        }

        return best;
    }

    /// <summary>The nearest spot in a direction from a point, or null when nothing lies that way.</summary>
    public static ScreenPoint? Next(IReadOnlyList<ScreenPoint> spots, ScreenPoint from, GridDirection direction)
    {
        ScreenPoint? best = null;
        double bestCost = double.MaxValue;

        foreach (ScreenPoint spot in spots)
        {
            (int along, int across) = Offset(from, spot, direction);

            if (along < Level || along < across * Cone)
            {
                continue;
            }

            double cost = along + across * 3.0;

            if (cost < bestCost)
            {
                best = spot;
                bestCost = cost;
            }
        }

        return best;
    }

    private static (int Along, int Across) Offset(ScreenPoint from, ScreenPoint to, GridDirection direction)
    {
        int sideways = Math.Abs(to.X - from.X);
        int upright = Math.Abs(to.Y - from.Y);

        switch (direction)
        {
            case GridDirection.Up:
                return (from.Y - to.Y, sideways);
            case GridDirection.Down:
                return (to.Y - from.Y, sideways);
            case GridDirection.Left:
                return (from.X - to.X, upright);
            default:
                return (to.X - from.X, upright);
        }
    }
}
