namespace GDPilot.Core;

/// <summary>
/// How one kind of spot looks: a bright disc of DiscRadius pixels standing out
/// by at least MinimumContrast from a ring RingRadius out, with no two spots
/// closer than Spacing.
/// </summary>
public sealed record SpotLook(int DiscRadius, int RingRadius, double MinimumContrast, int Spacing)
{
    /// <summary>
    /// A skill button at 1920 by 1080, UI scale 0.5: an icon about 36 pixels
    /// across in a dark tree. The darkest button seen, an unlearned Shaman
    /// skill, stands out by 52; the brightest false find, a level label, by 44.
    /// Buttons are never closer than 70 pixels, so anything within 45 of a
    /// stronger find is its label or marker.
    /// </summary>
    public static SpotLook SkillButton { get; } = new(12, 26, 48, 45);

    /// <summary>How far apart a constellation's stars sit, for the star finder; its other sizes are its own.</summary>
    public static SpotLook DevotionStar { get; } = new(4, 12, 60, 20);
}

/// <summary>
/// Finds the stars of the devotion map. A star not taken is a point of light,
/// brighter at its middle than anything a few pixels out, dim or in a nebula;
/// the outlines of a lit constellation run on past that as bright as they
/// began. A star taken is a blaze, bright well out from its middle, which no
/// outline is. Measured on 2026-11-05 at 1920 by 1080, UI scale 0.5, against
/// lit and unlit constellations: points 126 and up at their middle with at most
/// 0.87 of that seven pixels out, where outlines but one reached 1.05 and up;
/// blazes 181 and up over a radius of four, with 170 and up seven out. A star
/// flaring on an outline of its own, as the reachable constellations' do, keeps a
/// solid core: 54 and up all round three pixels out, where a line's falls to 44.
/// </summary>
public static class StarFinder
{
    private const double PointBrightest = 100;
    private const double FlareFrom = 100;
    private const double FlareCoreFrom = 72;
    private const double FlareStandsOut = 0.7;
    private const int FlareCoreRadius = 3;
    private const int ClumpReach = 70;
    private const int ClumpMost = 2;
    private const double PointFrom = 120;
    private const double PointStandsOut = 0.88;
    private const double BlazeFrom = 175;
    private const double BlazeRingFrom = 165;
    private const int RingRadius = 7;
    private const int RingPoints = 24;

    public static List<ScreenPoint> Find(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int left, int top, int width, int height, int spacing)
    {
        int margin = RingRadius + 1;
        int spanX = width + margin * 2;
        int spanY = height + margin * 2;
        float[] brightness = new float[spanX * spanY];

        for (int y = 0; y < spanY; y++)
        {
            for (int x = 0; x < spanX; x++)
            {
                (byte red, byte green, byte blue) = pixel(left - margin + x, top - margin + y);
                brightness[y * spanX + x] = (red + green + blue) / 3f;
            }
        }

        int[] inner = Disc(2, spanX);
        int[] wide = Disc(4, spanX);
        int[] core = Circle(FlareCoreRadius, spanX);
        int[] ring = new int[RingPoints];

        for (int index = 0; index < RingPoints; index++)
        {
            double angle = index * Math.PI * 2 / RingPoints;
            ring[index] = (int)Math.Round(RingRadius * Math.Sin(angle)) * spanX + (int)Math.Round(RingRadius * Math.Cos(angle));
        }

        List<(double Score, int X, int Y, bool FlareOnly)> candidates = new();
        double[] around = new double[RingPoints];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int centre = (y + margin) * spanX + x + margin;

                // Almost all of the map is far too dim to be the middle of anything.
                if (brightness[centre] < PointBrightest)
                {
                    continue;
                }

                double middle = Mean(brightness, centre, inner);

                if (middle < FlareFrom)
                {
                    continue;
                }

                for (int index = 0; index < RingPoints; index++)
                {
                    around[index] = brightness[centre + ring[index]];
                }

                Array.Sort(around);
                bool point = middle >= PointFrom && around[RingPoints - 1] <= middle * PointStandsOut;
                bool flare = around[RingPoints / 2] <= middle * FlareStandsOut && Lowest(brightness, centre, core) >= FlareCoreFrom;
                bool blaze = Mean(brightness, centre, wide) >= BlazeFrom && around[RingPoints / 2] >= BlazeRingFrom;

                if (point || flare || blaze)
                {
                    candidates.Add((middle, left + x, top + y, flare && !point && !blaze));
                }
            }
        }

        candidates.Sort((first, second) => second.Score.CompareTo(first.Score));
        List<ScreenPoint> stars = new();
        List<ScreenPoint> flares = new();
        int apart = spacing * spacing;

        foreach ((double _, int x, int y, bool flareOnly) in candidates)
        {
            bool clear = true;

            foreach (ScreenPoint star in stars)
            {
                int dx = star.X - x;
                int dy = star.Y - y;

                if (dx * dx + dy * dy <= apart)
                {
                    clear = false;
                    break;
                }
            }

            if (clear)
            {
                stars.Add(new ScreenPoint(x, y));

                if (flareOnly)
                {
                    flares.Add(new ScreenPoint(x, y));
                }
            }
        }

        // Stars stand apart, so finds of this weakest kind that crowd together are the detail of the artwork.
        foreach (ScreenPoint flare in flares)
        {
            if (flares.Count(other => Within(flare, other, ClumpReach)) > ClumpMost)
            {
                stars.Remove(flare);
            }
        }

        return stars;
    }

    private static bool Within(ScreenPoint first, ScreenPoint second, int reach)
    {
        int dx = first.X - second.X;
        int dy = first.Y - second.Y;
        return dx * dx + dy * dy <= reach * reach;
    }

    private static double Lowest(float[] brightness, int centre, int[] offsets)
    {
        double lowest = double.MaxValue;

        foreach (int offset in offsets)
        {
            lowest = Math.Min(lowest, brightness[centre + offset]);
        }

        return lowest;
    }

    private static int[] Circle(int radius, int stride)
    {
        int[] offsets = new int[12];

        for (int index = 0; index < offsets.Length; index++)
        {
            double angle = index * Math.PI * 2 / offsets.Length;
            offsets[index] = (int)Math.Round(radius * Math.Sin(angle)) * stride + (int)Math.Round(radius * Math.Cos(angle));
        }

        return offsets;
    }

    private static double Mean(float[] brightness, int centre, int[] offsets)
    {
        double sum = 0;

        foreach (int offset in offsets)
        {
            sum += brightness[centre + offset];
        }

        return sum / offsets.Length;
    }

    private static int[] Disc(int radius, int stride)
    {
        List<int> offsets = new();

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx * dx + dy * dy <= radius * radius)
                {
                    offsets.Add(dy * stride + dx);
                }
            }
        }

        return offsets.ToArray();
    }
}

/// <summary>
/// Finds spots on screen by their look alone. The game does not say where its
/// menu buttons are, but a skill button is a bright icon in a darker tree, so
/// a bright disc with a dark surround is a button. The surround is judged by
/// its brighter quarter, so a connecting line or a label below the button does
/// not count against it, while plain text, thin and broken, never fills a disc.
/// </summary>
public static class SpotFinder
{
    private const int Stride = 2;
    private const int RingPoints = 32;

    /// <summary>More ring points brighter than the limit than this, and the brighter quarter is over it.</summary>
    private const int LitAllowed = RingPoints - RingPoints * 3 / 4;

    public static List<ScreenPoint> Find(Func<int, int, (byte Red, byte Green, byte Blue)> pixel, int left, int top, int width, int height, SpotLook look)
    {
        int margin = look.RingRadius + 1;
        int spanX = width + margin * 2;
        int spanY = height + margin * 2;
        // Kept as one flat row after another, with each sample a fixed step from the centre, since this runs at a press.
        float[] brightness = new float[spanX * spanY];

        for (int y = 0; y < spanY; y++)
        {
            for (int x = 0; x < spanX; x++)
            {
                (byte red, byte green, byte blue) = pixel(left - margin + x, top - margin + y);
                brightness[y * spanX + x] = (red + green + blue) / 3f;
            }
        }

        int[] disc = DiscOffsets(look.DiscRadius).Select(offset => offset.Y * spanX + offset.X).ToArray();
        int[] ring = RingOffsets(look.RingRadius).Select(offset => offset.Y * spanX + offset.X).ToArray();
        List<(double Score, int X, int Y)> candidates = new();
        double[] around = new double[ring.Length];

        for (int y = 0; y < height; y += Stride)
        {
            for (int x = 0; x < width; x += Stride)
            {
                int centre = (y + margin) * spanX + x + margin;
                double inside = 0;

                foreach (int offset in disc)
                {
                    inside += brightness[centre + offset];
                }

                inside /= disc.Length;

                // Most of a menu is dark background, which cannot reach the contrast even against black.
                if (inside < look.MinimumContrast)
                {
                    continue;
                }

                // The surround passes only if its brighter quarter stays dim enough, so stop counting once too much of it is lit.
                double limit = inside - look.MinimumContrast;
                int lit = 0;

                for (int index = 0; index < ring.Length && lit < LitAllowed; index++)
                {
                    around[index] = brightness[centre + ring[index]];

                    if (around[index] > limit)
                    {
                        lit++;
                    }
                }

                if (lit >= LitAllowed)
                {
                    continue;
                }

                Array.Sort(around);
                double score = inside - around[around.Length * 3 / 4];

                if (score >= look.MinimumContrast)
                {
                    candidates.Add((score, left + x, top + y));
                }
            }
        }

        // Strongest first, so each spot is seeded by its best-placed centre and its weaker neighbours join it.
        candidates.Sort((first, second) => second.Score.CompareTo(first.Score));
        List<Plateau> plateaus = new();
        int apart = look.Spacing * look.Spacing;

        foreach ((double score, int x, int y) in candidates)
        {
            Plateau? owner = null;

            foreach (Plateau plateau in plateaus)
            {
                int dx = plateau.SeedX - x;
                int dy = plateau.SeedY - y;

                if (dx * dx + dy * dy <= apart)
                {
                    owner = plateau;
                    break;
                }
            }

            if (owner is null)
            {
                plateaus.Add(new Plateau(score, x, y));
            }
            else
            {
                owner.Join(score, x, y);
            }
        }

        return plateaus.Select(plateau => plateau.Centre).ToList();
    }

    /// <summary>
    /// The positions scoring nearly as well as a spot's best. A flat button
    /// scores the same over a small area, so its centre is the middle of that
    /// area, not whichever position happened to be read first.
    /// </summary>
    private sealed class Plateau
    {
        private const double NearlyAsGood = 0.9;
        private readonly double best;
        private long sumX;
        private long sumY;
        private int count;

        public Plateau(double best, int x, int y)
        {
            this.best = best;
            SeedX = x;
            SeedY = y;
            sumX = x;
            sumY = y;
            count = 1;
        }

        public int SeedX { get; }

        public int SeedY { get; }

        public ScreenPoint Centre => new((int)Math.Round((double)sumX / count), (int)Math.Round((double)sumY / count));

        public void Join(double score, int x, int y)
        {
            if (score < best * NearlyAsGood)
            {
                return;
            }

            sumX += x;
            sumY += y;
            count++;
        }
    }

    private static List<(int X, int Y)> DiscOffsets(int radius)
    {
        List<(int X, int Y)> offsets = new();

        for (int dx = -radius; dx <= radius; dx += 3)
        {
            for (int dy = -radius; dy <= radius; dy += 3)
            {
                if (dx * dx + dy * dy <= radius * radius)
                {
                    offsets.Add((dx, dy));
                }
            }
        }

        return offsets;
    }

    private static List<(int X, int Y)> RingOffsets(int radius)
    {
        List<(int X, int Y)> offsets = new();

        for (int index = 0; index < RingPoints; index++)
        {
            double angle = index * Math.PI * 2 / RingPoints;
            offsets.Add(((int)Math.Round(radius * Math.Cos(angle)), (int)Math.Round(radius * Math.Sin(angle))));
        }

        return offsets;
    }
}
