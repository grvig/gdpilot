using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class LegendFoldTests
{
    private static readonly ButtonLayers Combat = BindingTable.Parse(new Dictionary<string, string> { ["A"] = "key:Space", ["B"] = "key:4" }, 0.5);

    [Fact]
    public void TheButtonsShowForAWhileThenFold()
    {
        LegendFold fold = new();
        OverlayState state = OverlayState.Build(Mode.Combat, SuspendReason.None, Combat);

        Assert.False(fold.Folded(state, TimeSpan.Zero));
        Assert.False(fold.Folded(state, TimeSpan.FromSeconds(4)));
        Assert.True(fold.Folded(state, TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void AChangeOfModeOpensTheCardAgain()
    {
        LegendFold fold = new();
        fold.Folded(OverlayState.Build(Mode.Combat, SuspendReason.None, Combat), TimeSpan.Zero);
        fold.Folded(OverlayState.Build(Mode.Combat, SuspendReason.None, Combat), TimeSpan.FromSeconds(6));

        Assert.False(fold.Folded(OverlayState.Build(Mode.Inventory, SuspendReason.None, Combat), TimeSpan.FromMilliseconds(4)));
    }

    [Fact]
    public void FoldedTheCardKeepsOnlyItsStatusLines()
    {
        OverlayState state = OverlayState.Build(Mode.Combat, SuspendReason.None, Combat, thirdPerson: true) with { Folded = true };

        Assert.Equal(new[] { "Camera: third person; right stick turns and zooms" }, state.Shown);
        Assert.Equal(3, (state with { Folded = false }).Shown.Count);
    }
}
