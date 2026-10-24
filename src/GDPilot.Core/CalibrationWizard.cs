namespace GDPilot.Core;

public enum WizardStep
{
    PointAtFirstCell,
    PointAtLastCell,
    CountCells,
    Review,
    Done,
    Cancelled,
}

/// <summary>
/// Section 11's calibration, as steps a pad can drive: point at the centre of
/// the top-left cell and confirm, then the bottom-right cell. Where grid lines
/// can be read between those two points the grid is detected and the person
/// only confirms it. Where they cannot, the cell counts are set by hand and the
/// pitch follows from the corners.
/// </summary>
public sealed class CalibrationWizard
{
    /// <summary>
    /// How far past the pointed cell centres the detector looks, to take in the
    /// outer lines. Too little misses them and too much takes in the panel's
    /// border, and the right amount grows with the UI scale, so each is tried in
    /// turn until one finds the grid that was pointed at.
    /// </summary>
    private static readonly int[] SearchMargins = { 20, 32, 48, 64 };

    /// <summary>Half the smallest cell: two points closer than this in one direction are a single row or column.</summary>
    private const int LevelSlack = 8;

    private readonly Func<ScreenRect, GridSpec?> detect;
    private (int X, int Y) first;
    private (int X, int Y) last;

    public CalibrationWizard(Func<ScreenRect, GridSpec?> detect)
    {
        this.detect = detect;
    }

    public WizardStep Step { get; private set; } = WizardStep.PointAtFirstCell;

    /// <summary>The grid found or built, once the wizard reaches review.</summary>
    public GridSpec? Result { get; private set; }

    /// <summary>The counts being set by hand in the count step.</summary>
    public (int Columns, int Rows) Counts { get; private set; } = (2, 2);

    public string Prompt
    {
        get
        {
            switch (Step)
            {
                case WizardStep.PointAtFirstCell:
                    return "Move the cursor to the centre of the top-left cell and press A.";
                case WizardStep.PointAtLastCell:
                    return "Now the centre of the bottom-right cell, and press A.";
                case WizardStep.CountCells:
                    return $"Grid lines could not be read. Set the size with the D-pad: {Counts.Columns} columns, {Counts.Rows} rows. Press A when right.";
                case WizardStep.Review:
                    return $"Found {Result!.Columns} by {Result.Rows} cells, {Spacing(Result):0.#} pixels apart. Press A to save, B to start again.";
                case WizardStep.Done:
                    return "Saved.";
                default:
                    return "Calibration cancelled. Nothing was saved.";
            }
        }
    }

    public void Confirm((int X, int Y) cursor)
    {
        switch (Step)
        {
            case WizardStep.PointAtFirstCell:
                first = cursor;
                Step = WizardStep.PointAtLastCell;
                break;
            case WizardStep.PointAtLastCell:
                AcceptLastCell(cursor);
                break;
            case WizardStep.CountCells:
                Result = GridSpec.FromCorners(first.X, first.Y, last.X, last.Y, Counts.Columns, Counts.Rows);
                Step = WizardStep.Review;
                break;
            case WizardStep.Review:
                Step = WizardStep.Done;
                break;
        }
    }

    /// <summary>In the count step, changes the column and row counts, never below two unless the points were level.</summary>
    public void Adjust(int columns, int rows)
    {
        if (Step == WizardStep.CountCells)
        {
            Counts = (Math.Max(Fewest(first.X, last.X), Counts.Columns + columns), Math.Max(Fewest(first.Y, last.Y), Counts.Rows + rows));
        }
    }

    /// <summary>B: from review, starts again; anywhere else, gives up.</summary>
    public void Back()
    {
        if (Step == WizardStep.Review)
        {
            Result = null;
            Step = WizardStep.PointAtFirstCell;
            return;
        }

        Step = WizardStep.Cancelled;
    }

    private void AcceptLastCell((int X, int Y) cursor)
    {
        // Placed by stick, a tab strip's far end is never exactly level, so near enough is.
        if (Math.Abs(cursor.Y - first.Y) <= LevelSlack)
        {
            cursor.Y = first.Y;
        }

        if (Math.Abs(cursor.X - first.X) <= LevelSlack)
        {
            cursor.X = first.X;
        }

        // A bottom-right cell above or left of the top-left one was a slip; ask again.
        if (cursor.X < first.X || cursor.Y < first.Y || cursor == first)
        {
            return;
        }

        last = cursor;
        Counts = (Fewest(first.X, last.X), Fewest(first.Y, last.Y));

        foreach (int margin in SearchMargins)
        {
            ScreenRect area = new(first.X - margin, first.Y - margin, last.X - first.X + margin * 2, last.Y - first.Y + margin * 2);
            GridSpec? found = detect(area);

            if (found is not null && Agrees(found))
            {
                Result = found;
                Step = WizardStep.Review;
                return;
            }
        }

        Step = WizardStep.CountCells;
    }

    /// <summary>A single column's horizontal pitch is a placeholder, so its spacing is read downwards.</summary>
    private static double Spacing(GridSpec grid)
    {
        if (grid.Columns == 1)
        {
            return grid.PitchY;
        }

        return grid.PitchX;
    }

    private static int Fewest(int first, int last)
    {
        if (first == last)
        {
            return 1;
        }

        return 2;
    }

    /// <summary>
    /// A detected grid is trusted only if its first and last cells sit under the
    /// two points the person chose, within half a cell. Otherwise the detector
    /// latched onto something else, such as a neighbouring panel.
    /// </summary>
    private bool Agrees(GridSpec found)
    {
        (int firstX, int firstY) = found.Centre(new GridCell(0, 0));
        (int lastX, int lastY) = found.Centre(new GridCell(found.Columns - 1, found.Rows - 1));
        double slackX = found.PitchX / 2;
        double slackY = found.PitchY / 2;

        return Math.Abs(firstX - first.X) <= slackX
            && Math.Abs(firstY - first.Y) <= slackY
            && Math.Abs(lastX - last.X) <= slackX
            && Math.Abs(lastY - last.Y) <= slackY;
    }
}
