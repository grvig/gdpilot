using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class CalibrationWizardTests
{
    private static readonly GridSpec Bags = new(652, 645, 32, 32, 12, 8);

    private ScreenRect? searched;

    [Fact]
    public void PointingAtBothCornersOfADetectableGridGoesStraightToReview()
    {
        CalibrationWizard wizard = new(Detect(Bags));

        wizard.Confirm((650, 646));
        wizard.Confirm((1004, 869));

        Assert.Equal(WizardStep.Review, wizard.Step);
        Assert.Equal(Bags, wizard.Result);
        Assert.Contains("12 by 8", wizard.Prompt);
    }

    [Fact]
    public void TheDetectorIsAskedAboutTheAreaBetweenThePointsWithAMargin()
    {
        CalibrationWizard wizard = new(Detect(Bags));

        wizard.Confirm((652, 645));
        wizard.Confirm((1004, 869));

        Assert.Equal(new ScreenRect(604, 597, 448, 320), searched);
    }

    [Fact]
    public void ConfirmingTheReviewFinishes()
    {
        CalibrationWizard wizard = new(Detect(Bags));
        wizard.Confirm((652, 645));
        wizard.Confirm((1004, 869));

        wizard.Confirm((0, 0));

        Assert.Equal(WizardStep.Done, wizard.Step);
    }

    [Fact]
    public void WithNoReadableLinesTheCountsAreSetByHand()
    {
        CalibrationWizard wizard = new(Detect(null));
        wizard.Confirm((652, 645));
        wizard.Confirm((1004, 869));
        Assert.Equal(WizardStep.CountCells, wizard.Step);

        wizard.Adjust(10, 6);
        wizard.Confirm((0, 0));

        Assert.Equal(WizardStep.Review, wizard.Step);
        Assert.Equal(new GridSpec(652, 645, 32, 32, 12, 8), wizard.Result);
    }

    [Fact]
    public void ADetectedGridThatMissesThePointsIsNotTrusted()
    {
        // The neighbouring bag, found instead of the one pointed at.
        CalibrationWizard wizard = new(Detect(new GridSpec(1044, 645, 32, 32, 8, 8)));

        wizard.Confirm((652, 645));
        wizard.Confirm((1004, 869));

        Assert.Equal(WizardStep.CountCells, wizard.Step);
    }

    [Fact]
    public void ASecondPointAboveOrLeftOfTheFirstIsIgnored()
    {
        CalibrationWizard wizard = new(Detect(Bags));
        wizard.Confirm((652, 645));

        wizard.Confirm((600, 700));

        Assert.Equal(WizardStep.PointAtLastCell, wizard.Step);
    }

    [Fact]
    public void CountsNeverDropBelowTwo()
    {
        CalibrationWizard wizard = new(Detect(null));
        wizard.Confirm((652, 645));
        wizard.Confirm((1004, 869));

        wizard.Adjust(-5, -5);

        Assert.Equal((2, 2), wizard.Counts);
    }

    [Fact]
    public void NearlyLevelPointsMakeAOneRowTabStrip()
    {
        CalibrationWizard wizard = new(Detect(null));
        wizard.Confirm((520, 300));
        wizard.Confirm((701, 305));
        Assert.Equal((2, 1), wizard.Counts);

        wizard.Adjust(2, -1);
        wizard.Confirm((0, 0));

        Assert.Equal(new GridSpec(520, 300, 60.333333333333336, 1, 4, 1), wizard.Result);
    }

    [Fact]
    public void BackFromReviewStartsAgainAndBackElsewhereCancels()
    {
        CalibrationWizard wizard = new(Detect(Bags));
        wizard.Confirm((652, 645));
        wizard.Confirm((1004, 869));

        wizard.Back();
        Assert.Equal(WizardStep.PointAtFirstCell, wizard.Step);
        Assert.Null(wizard.Result);

        wizard.Back();
        Assert.Equal(WizardStep.Cancelled, wizard.Step);
    }

    private Func<ScreenRect, GridSpec?> Detect(GridSpec? found)
    {
        return area =>
        {
            searched = area;
            return found;
        };
    }
}
