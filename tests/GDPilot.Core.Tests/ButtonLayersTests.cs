using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class ButtonLayersTests
{
    private readonly ButtonLayers layers = new(
        new Dictionary<GamepadButtons, ButtonBinding>
        {
            [GamepadButtons.A] = new KeyBinding(KeyCode.Space),
            [GamepadButtons.X] = new PointerBinding(PointerButton.Left),
        },
        new Dictionary<GamepadButtons, ButtonBinding>
        {
            [GamepadButtons.A] = new KeyBinding(KeyCode.D1),
        },
        0.5);

    [Fact]
    public void BaseLayerAppliesWithTheTriggerReleased()
    {
        (HashSet<KeyCode> keys, HashSet<PointerButton> buttons) = layers.Resolve(Pressed(GamepadButtons.A | GamepadButtons.X, 0));

        Assert.Equal(new[] { KeyCode.Space }, keys);
        Assert.Equal(new[] { PointerButton.Left }, buttons);
    }

    [Fact]
    public void ChordLayerAppliesWhileTheTriggerIsHeld()
    {
        (HashSet<KeyCode> keys, _) = layers.Resolve(Pressed(GamepadButtons.A, 0.8));

        Assert.Equal(new[] { KeyCode.D1 }, keys);
    }

    [Fact]
    public void TheChordLayerReplacesTheBaseLayerOutright()
    {
        // X has no chord binding, so with the trigger held it does nothing.
        (_, HashSet<PointerButton> buttons) = layers.Resolve(Pressed(GamepadButtons.X, 0.8));

        Assert.Empty(buttons);
    }

    [Fact]
    public void UnboundButtonsDoNothing()
    {
        (HashSet<KeyCode> keys, HashSet<PointerButton> buttons) = layers.Resolve(Pressed(GamepadButtons.Y, 0));

        Assert.Empty(keys);
        Assert.Empty(buttons);
    }

    private static GamepadSnapshot Pressed(GamepadButtons buttons, double leftTrigger)
    {
        return GamepadSnapshot.Rest with { Buttons = buttons, LeftTrigger = leftTrigger };
    }
}
