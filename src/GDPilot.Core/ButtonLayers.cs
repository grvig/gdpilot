namespace GDPilot.Core;

/// <summary>What a pad button does: hold a key, or hold a mouse button.</summary>
public abstract record ButtonBinding;

public sealed record KeyBinding(KeyCode Key) : ButtonBinding;

public sealed record PointerBinding(PointerButton Button) : ButtonBinding;

/// <summary>
/// The flat button table plus the chord layer section 9 asks for. While the left
/// trigger is held the chord table replaces the base table outright, which
/// roughly doubles the inputs a pad can express.
/// </summary>
public sealed class ButtonLayers
{
    private readonly IReadOnlyDictionary<GamepadButtons, ButtonBinding> baseLayer;
    private readonly IReadOnlyDictionary<GamepadButtons, ButtonBinding> chordLayer;
    private readonly double triggerThreshold;

    public ButtonLayers(
        IReadOnlyDictionary<GamepadButtons, ButtonBinding> baseLayer,
        IReadOnlyDictionary<GamepadButtons, ButtonBinding> chordLayer,
        double triggerThreshold)
    {
        this.baseLayer = baseLayer;
        this.chordLayer = chordLayer;
        this.triggerThreshold = triggerThreshold;
    }

    public (HashSet<KeyCode> Keys, HashSet<PointerButton> Buttons) Resolve(GamepadSnapshot snapshot)
    {
        IReadOnlyDictionary<GamepadButtons, ButtonBinding> layer = baseLayer;

        if (snapshot.LeftTrigger >= triggerThreshold)
        {
            layer = chordLayer;
        }

        HashSet<KeyCode> keys = new();
        HashSet<PointerButton> buttons = new();

        foreach ((GamepadButtons button, ButtonBinding binding) in layer)
        {
            if (!snapshot.IsPressed(button))
            {
                continue;
            }

            if (binding is KeyBinding key)
            {
                keys.Add(key.Key);
            }
            else if (binding is PointerBinding pointer)
            {
                buttons.Add(pointer.Button);
            }
        }

        return (keys, buttons);
    }
}
