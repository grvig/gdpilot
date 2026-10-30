using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class DodgeAimTests
{
    private const double Aspect = 16.0 / 9;

    // The ring as a low third-person camera sees it: ahead is far up the screen,
    // behind is close below, and the sides spread wide.
    private static readonly TargetsReport Near = new(Array.Empty<SeenTarget>())
    {
        Feet = new ScreenSpot(0.5, 0.7),
        Ring = new ScreenSpot?[]
        {
            new ScreenSpot(0.5, 0.45),
            new ScreenSpot(0.62, 0.5),
            new ScreenSpot(0.75, 0.68),
            new ScreenSpot(0.66, 0.85),
            null,
            new ScreenSpot(0.34, 0.85),
            new ScreenSpot(0.25, 0.68),
            new ScreenSpot(0.38, 0.5),
        },
    };

    [Fact]
    public void TheStickRightAimsAtTheGroundToTheRight()
    {
        Assert.Equal(new ScreenSpot(0.75, 0.68), DodgeAim.Spot(Near, new StickPosition(1, 0), 0.25, Aspect));
    }

    [Fact]
    public void TheStickUpAndLeftAimsAheadAndLeft()
    {
        Assert.Equal(new ScreenSpot(0.38, 0.5), DodgeAim.Spot(Near, new StickPosition(-0.7, 0.7), 0.25, Aspect));
    }

    [Fact]
    public void AtRestTheDodgeGoesForward()
    {
        Assert.Equal(new ScreenSpot(0.5, 0.45), DodgeAim.Spot(Near, StickPosition.Centre, 0.25, Aspect));
    }

    [Fact]
    public void APointBehindTheCameraIsPassedOverForItsNeighbour()
    {
        ScreenSpot spot = DodgeAim.Spot(Near, new StickPosition(0, -1), 0.25, Aspect);

        Assert.True(spot == new ScreenSpot(0.66, 0.85) || spot == new ScreenSpot(0.34, 0.85));
    }

    [Fact]
    public void WithoutTheGameTheDodgeAimsOutFromTheMiddle()
    {
        Assert.Equal(new ScreenSpot(0.5, 0.3), DodgeAim.Spot(null, StickPosition.Centre, 0.25, Aspect));
    }

    [Fact]
    public void ADodgeBindingNamesItsKeyAndReadsPlainly()
    {
        ButtonLayers combat = BindingTable.Parse(new Dictionary<string, string> { ["B"] = "dodge:4" }, 0.5);

        Assert.Equal(new DodgeBinding(KeyCode.D4), combat.BindingFor(GamepadButtons.B, 0));
        Assert.Contains("B: Dodge (4)", OverlayState.Build(Mode.Combat, SuspendReason.None, combat).Legend);
    }
}
