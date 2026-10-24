namespace GDPilot.Core;

/// <summary>
/// A point on the ground round the character at any angle, for aiming with a
/// stick: the two points of the game's ring either side of the wanted direction
/// are blended by angle, then the point is drawn in or pushed out from the feet.
/// The ring is six world units out, so a reach of 1 is six units.
/// </summary>
public static class GroundAim
{
    /// <summary>Without a report from the game, a reach of 1 is this far from the middle of the window, in window heights.</summary>
    private const double Fallback = 0.2;

    /// <param name="angle">The screen direction wanted, in radians, with up the screen at minus a quarter turn.</param>
    /// <param name="reach">How far out, as a share of the ring's six units.</param>
    public static ScreenSpot Spot(TargetsReport? near, double angle, double reach, double aspect)
    {
        if (near?.Feet is ScreenSpot feet && Blend(near.Ring, feet, angle, aspect) is ScreenSpot onRing)
        {
            return new ScreenSpot(feet.X + (onRing.X - feet.X) * reach, feet.Y + (onRing.Y - feet.Y) * reach);
        }

        return new ScreenSpot(0.5 + Math.Cos(angle) * Fallback * reach / aspect, 0.5 + Math.Sin(angle) * Fallback * reach);
    }

    /// <summary>The ring's point at the wanted angle, blended from its neighbours either side; null without two points to blend.</summary>
    private static ScreenSpot? Blend(IReadOnlyList<ScreenSpot?> ring, ScreenSpot feet, double angle, double aspect)
    {
        List<(double Angle, ScreenSpot Point)> around = new();

        foreach (ScreenSpot? point in ring)
        {
            if (point is not null)
            {
                around.Add((Math.Atan2(point.Y - feet.Y, (point.X - feet.X) * aspect), point));
            }
        }

        if (around.Count < 2)
        {
            return null;
        }

        // The nearest point clockwise of the wanted angle, and the nearest anticlockwise.
        (double Angle, ScreenSpot Point)? before = null;
        (double Angle, ScreenSpot Point)? after = null;
        double beforeGap = double.MaxValue;
        double afterGap = double.MaxValue;

        foreach ((double pointAngle, ScreenSpot point) in around)
        {
            double gap = Turn(pointAngle - angle);

            if (gap <= 0 && -gap < beforeGap)
            {
                before = (pointAngle, point);
                beforeGap = -gap;
            }

            if (gap > 0 && gap < afterGap)
            {
                after = (pointAngle, point);
                afterGap = gap;
            }
        }

        if (before is not (double _, ScreenSpot from))
        {
            return after!.Value.Point;
        }

        if (after is not (double _, ScreenSpot to))
        {
            return from;
        }

        double share = beforeGap / (beforeGap + afterGap);
        return new ScreenSpot(from.X + (to.X - from.X) * share, from.Y + (to.Y - from.Y) * share);
    }

    /// <summary>An angle brought between minus and plus half a turn.</summary>
    private static double Turn(double angle)
    {
        return Math.IEEERemainder(angle, 2 * Math.PI);
    }
}
