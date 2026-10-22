using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class ModifierClickTests
{
    private static readonly ButtonLayers Layers = BindingTable.Parse(
        new Dictionary<string, string>
        {
            ["A"] = "mouse:left",
            ["LT+A"] = "mouse:ctrl+left",
            ["X"] = "mouse:Shift + Right",
        },
        0.5);

    [Fact]
    public void AModifiedClickParsesWithItsModifier()
    {
        Assert.Equal(new PointerBinding(PointerButton.Left, KeyCode.LeftControl), Layers.BindingFor(GamepadButtons.A, 1.0));
        Assert.Equal(new PointerBinding(PointerButton.Right, KeyCode.LeftShift), Layers.BindingFor(GamepadButtons.X, 0));
        Assert.Equal(new PointerBinding(PointerButton.Left), Layers.BindingFor(GamepadButtons.A, 0));
    }

    [Fact]
    public void TheModifierGoesDownBeforeTheButtonAndUpAfterIt()
    {
        HeldInputDiffer differ = new();
        GamepadSnapshot held = GamepadSnapshot.Rest with { Buttons = GamepadButtons.A, LeftTrigger = 1.0 };

        (HashSet<KeyCode> keys, HashSet<PointerButton> buttons) = Layers.Resolve(held);
        List<InputIntent> press = differ.Update(keys, buttons);
        List<InputIntent> release = differ.Update(new HashSet<KeyCode>(), new HashSet<PointerButton>());

        Assert.Equal(new InputIntent[] { new KeyDown(KeyCode.LeftControl), new PointerDown(PointerButton.Left) }, press);
        Assert.Equal(new InputIntent[] { new PointerUp(PointerButton.Left), new KeyUp(KeyCode.LeftControl) }, release);
    }

    [Fact]
    public void TheLegendNamesTheModifier()
    {
        OverlayState state = OverlayState.Build(Mode.Inventory, SuspendReason.None, Layers);

        Assert.Contains("LT+A: Ctrl+Left click", state.Legend);
        Assert.Contains("X: Shift+Right click", state.Legend);
    }

    [Theory]
    [InlineData("mouse:ctrl+shift+left")]
    [InlineData("mouse:hyper+left")]
    [InlineData("mouse:ctrl+middle")]
    public void MalformedModifiedClicksAreRejected(string action)
    {
        Assert.Throws<FormatException>(() => BindingTable.Parse(new Dictionary<string, string> { ["A"] = action }, 0.5));
    }
}
