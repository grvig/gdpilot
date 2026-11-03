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
