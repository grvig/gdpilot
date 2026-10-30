namespace GDPilot.Core;

/// <summary>"dodge:4": taps the key with the cursor put a few steps out the way the left stick points.</summary>
public sealed record DodgeBinding(KeyCode Key) : ButtonBinding;

/// <summary>
/// Where a dodge aims: the point of ground the left stick points at, a few
/// steps out from the character. Grim Dawn's movement skills go toward the
/// cursor, so putting the cursor there for the press turns any of them into a
/// dodge in the stick's direction, as a game made for a controller would do it.
/// </summary>
public static class DodgeAim
{
    /// <summary>Without a report from the game, the spot this far from the middle of the window, in window heights.</summary>
    private const double Fallback = 0.2;

    /// <summary>
    /// The spot, as fractions of the game window. The stick's up is the camera's
    /// forward, which is the screen's up in either camera; with the stick at rest
    /// the dodge goes forward. The ring's points are matched by the way each lies
    /// from the feet on the screen, so perspective is the game's own.
    /// </summary>
    public static ScreenSpot Spot(TargetsReport? near, StickPosition left, double walkThreshold, double aspect)
    {
        double wantedX = left.X;
        double wantedY = -left.Y;

        if (left.Magnitude < walkThreshold)
        {
            wantedX = 0;
            wantedY = -1;
        }

        double wanted = Math.Atan2(wantedY, wantedX);

        if (near?.Feet is ScreenSpot feet)
        {
            ScreenSpot? best = null;
            double bestApart = double.MaxValue;

            foreach (ScreenSpot? point in near.Ring)
            {
                if (point is null)
                {
                    continue;
                }

                double lies = Math.Atan2(point.Y - feet.Y, (point.X - feet.X) * aspect);
                double apart = Math.Abs(Math.IEEERemainder(lies - wanted, 2 * Math.PI));

                if (apart < bestApart)
                {
                    best = point;
                    bestApart = apart;
                }
            }

            if (best is not null)
            {
                return best;
            }
        }

        double length = Math.Sqrt(wantedX * wantedX + wantedY * wantedY);
        return new ScreenSpot(0.5 + wantedX / length * Fallback / aspect, 0.5 + wantedY / length * Fallback);
    }
}
