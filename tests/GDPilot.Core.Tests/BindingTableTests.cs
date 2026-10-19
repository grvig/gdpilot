using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class BindingTableTests
{
    [Fact]
    public void ThePlansExampleParses()
    {
        ButtonLayers layers = Parse(("A", "key:Space"), ("LT+A", "key:1"));

        Assert.Equal(new[] { KeyCode.Space }, layers.Resolve(Pressed(GamepadButtons.A, 0)).Keys);
        Assert.Equal(new[] { KeyCode.D1 }, layers.Resolve(Pressed(GamepadButtons.A, 1)).Keys);
    }

    [Fact]
    public void MouseButtonsParse()
    {
        ButtonLayers layers = Parse(("RightBumper", "mouse:left"), ("lt+RightBumper", "mouse:right"));

        Assert.Equal(new[] { PointerButton.Left }, layers.Resolve(Pressed(GamepadButtons.RightBumper, 0)).Buttons);
        Assert.Equal(new[] { PointerButton.Right }, layers.Resolve(Pressed(GamepadButtons.RightBumper, 1)).Buttons);
    }

    [Fact]
    public void PanelAndBackActionsParse()
    {
        ButtonLayers layers = Parse(("DPadUp", "panel:inventory"), ("B", " Back "));

        Assert.Equal(new PanelBinding(Mode.Inventory), layers.BindingFor(GamepadButtons.DPadUp, 0));
        Assert.Equal(new BackBinding(), layers.BindingFor(GamepadButtons.B, 0));
        Assert.Null(layers.BindingFor(GamepadButtons.Y, 0));
    }

    [Theory]
    [InlineData("A", "panel:Combat")]
    [InlineData("A", "panel:Suspended")]
    [InlineData("A", "panel:Lobby")]
    [InlineData("Turbo", "key:Space")]
    [InlineData("A, B", "key:Space")]
    [InlineData("A", "Space")]
    [InlineData("A", "key:NotAKey")]
    [InlineData("A", "key:Ctrl+Space")]
    [InlineData("A", "mouse:middle")]
    [InlineData("DPadRight", "grid:right")]
    public void MalformedBindingsAreRejected(string trigger, string action)
    {
        Assert.Throws<FormatException>(() => Parse((trigger, action)));
    }

    [Fact]
    public void BindingTheSameButtonTwiceIsRejected()
    {
        Assert.Throws<FormatException>(() => Parse(("A", "key:Space"), ("a", "key:E")));
    }

    private static ButtonLayers Parse(params (string Trigger, string Action)[] entries)
    {
        Dictionary<string, string> section = entries.ToDictionary(entry => entry.Trigger, entry => entry.Action);
        return BindingTable.Parse(section, 0.5);
    }

    private static GamepadSnapshot Pressed(GamepadButtons buttons, double leftTrigger)
    {
        return GamepadSnapshot.Rest with { Buttons = buttons, LeftTrigger = leftTrigger };
    }
}
