namespace GDPilot.Core;

/// <summary>A row of evenly spaced lines: where the first sits, how far apart they are, and how many there are.</summary>
public readonly record struct LineRun(double First, double Spacing, int Count);

/// <summary>
/// Finds a grid from brightness alone. Grim Dawn draws empty cells dark with
/// thin bright edges, and item frames are drawn on the same edges, so averaging
/// brightness along a strip across the grid gives sharp peaks one cell apart.
/// The longest evenly spaced run of peaks is the grid.
/// </summary>
public static class GridDetector
{
    /// <summary>A peak must stand this far above the brightness around it.</summary>
    private const double Contrast = 1.25;

    /// <summary>How far either side of a pixel its surroundings reach, for judging a peak.</summary>
    private const int Surroundings = 8;

    /// <summary>
    /// How far a peak may sit from its expected place in a run, in pixels. A
    /// grid's outermost lines lie on the panel's thicker frame, whose brightest
    /// pixel can sit a couple of pixels off the line itself.
    /// </summary>
    private const double Tolerance = 2.5;

    /// <summary>
    /// A run must find a real peak at this share of its positions. Lines hidden
    /// under item art leave a few gaps; a run at half the true pitch misses at
    /// every other position and falls well short.
    /// </summary>
    private const double RequiredHits = 0.75;

    public static LineRun? FindLines(IReadOnlyList<double> profile, int minimumSpacing, int maximumSpacing)
    {
        List<int> peaks = Peaks(profile);
        LineRun? best = null;
        int bestHits = 0;

        for (int first = 0; first < peaks.Count; first++)
        {
            for (int second = first + 1; second < peaks.Count; second++)
            {
                int spacing = peaks[second] - peaks[first];

                if (spacing < minimumSpacing)
                {
                    continue;
                }

                if (spacing > maximumSpacing)
                {
                    break;
                }

                // Walked both ways, so a run found mid-grid still reaches its first line.
                (int forwardLines, int forwardHits, double _) = Run(peaks, peaks[first], spacing);
                (int backLines, int backHits, double start) = Run(peaks, peaks[first], -spacing);
                int lines = forwardLines + backLines - 1;
                int hits = forwardHits + backHits - 1;

                if (hits < lines * RequiredHits)
                {
                    continue;
                }

                // Longer runs win, then better-supported ones; between equals the
                // tighter spacing is the real grid, since twice the pitch is also even.
                bool better = best is null
                    || lines > best.Value.Count
                    || (lines == best.Value.Count && hits > bestHits)
                    || (lines == best.Value.Count && hits == bestHits && spacing < best.Value.Spacing);

                if (better)
                {
                    best = new LineRun(start, spacing, lines);
                    bestHits = hits;
                }
            }
        }

        if (best is null || best.Value.Count < 3)
        {
            return null;
        }

        return Coarsen(profile, best.Value, maximumSpacing);
    }

    /// <summary>
    /// Item backgrounds carry faint lines halfway across each cell, so a full
    /// grid also reads as an even run at half its pitch. Real cell borders are
    /// brighter than those halves. When alternate lines of the run are clearly
    /// unequal, the stronger set at double the spacing is the grid. A genuine
    /// fine grid has even lines and is left as it is.
    /// </summary>
    private static LineRun Coarsen(IReadOnlyList<double> profile, LineRun run, int maximumSpacing)
    {
        if (run.Spacing * 2 > maximumSpacing || run.Count < 5)
        {
            return run;
        }

        // Artefacts alternate consistently: weak, strong, weak, strong. A real grid
        // can have a dim line or two, so only a pattern held by nearly every
        // neighbouring pair counts, never an average a single line could tip.
        int evenStronger = 0;
        int oddStronger = 0;

        for (int line = 0; line < run.Count - 1; line++)
        {
            double here = Strength(profile, run.First + line * run.Spacing);
            double next = Strength(profile, run.First + (line + 1) * run.Spacing);
            int strongerParity = -1;

            if (here > next * Unequal)
            {
                strongerParity = line % 2;
            }
            else if (next > here * Unequal)
            {
                strongerParity = (line + 1) % 2;
            }

            if (strongerParity == 0)
            {
                evenStronger++;
            }
            else if (strongerParity == 1)
            {
                oddStronger++;
            }
        }

        int pairs = run.Count - 1;

        if (evenStronger >= pairs * Consistent)
        {
            return new LineRun(run.First, run.Spacing * 2, (run.Count + 1) / 2);
        }

        if (oddStronger >= pairs * Consistent)
        {
            return new LineRun(run.First + run.Spacing, run.Spacing * 2, run.Count / 2);
        }

        return run;
    }

    /// <summary>Neighbouring lines this far apart in strength are unequal.</summary>
    private const double Unequal = 1.15;

    /// <summary>The share of neighbouring pairs that must agree before half-cell artefacts are assumed.</summary>
    private const double Consistent = 0.8;

    /// <summary>The brightest value within the tolerance of a line's expected position.</summary>
    private static double Strength(IReadOnlyList<double> profile, double position)
    {
        double strongest = 0;

        for (int index = (int)Math.Floor(position - Tolerance); index <= (int)Math.Ceiling(position + Tolerance); index++)
        {
            if (index >= 0 && index < profile.Count)
            {
                strongest = Math.Max(strongest, profile[index]);
            }
        }

        return strongest;
    }

    /// <summary>
    /// A grid from its column lines and row lines, both measured from the same
    /// origin offset. Cells sit between lines, so a grid of n lines has n - 1 cells.
    /// </summary>
    public static GridSpec ToGrid(LineRun columns, LineRun rows, int offsetX, int offsetY)
    {
        return new GridSpec(
            offsetX + columns.First + columns.Spacing / 2,
            offsetY + rows.First + rows.Spacing / 2,
            columns.Spacing,
            rows.Spacing,
            columns.Count - 1,
            rows.Count - 1);
    }

    /// <summary>
    /// Local maxima that stand out from their own surroundings. Judging against
    /// the whole strip would lose every line in a bag full of bright items.
    /// </summary>
    private static List<int> Peaks(IReadOnlyList<double> profile)
    {
        List<int> peaks = new();

        for (int index = 1; index < profile.Count - 1; index++)
        {
            bool local = profile[index] >= profile[index - 1] && profile[index] > profile[index + 1];

            if (!local)
            {
                continue;
            }

            double around = 0;
            int counted = 0;

            for (int offset = -Surroundings; offset <= Surroundings; offset++)
            {
                int neighbour = index + offset;

                if (Math.Abs(offset) > 1 && neighbour >= 0 && neighbour < profile.Count)
                {
                    around += profile[neighbour];
                    counted++;
                }
            }

            if (profile[index] > around / counted * Contrast)
            {
                peaks.Add(index);
            }
        }

        return peaks;
    }

    /// <summary>
    /// Walks a run from a line in one direction: how many positions it spans, at
    /// how many a real peak was found, and where its last line sits. One hidden
    /// line is stepped over; two in a row end it. The last line is the expected
    /// position rather than the peak, so a frame's offset pixel cannot skew it.
    /// </summary>
    private static (int Lines, int Hits, double Last) Run(List<int> peaks, int start, int step)
    {
        int lines = 1;
        int hits = 1;
        int misses = 0;
        double expected = start + step;
        double last = start;

        while (misses < 2)
        {
            if (peaks.Any(peak => Math.Abs(peak - expected) <= Tolerance))
            {
                lines += 1 + misses;
                hits++;
                misses = 0;
                last = expected;
            }
            else
            {
                misses++;
            }

            expected += step;
        }

        return (lines, hits, last);
    }
}
