namespace GDPilot.Core;

/// <summary>What a pad button does: hold a key, or hold a mouse button.</summary>
public abstract record ButtonBinding;

public sealed record KeyBinding(KeyCode Key) : ButtonBinding;

/// <summary>Holds a mouse button, optionally with a modifier key held for the whole click.</summary>
public sealed record PointerBinding(PointerButton Button, KeyCode Modifier = KeyCode.None) : ButtonBinding;

/// <summary>Opens or closes a panel: taps its key and toggles its mode. Acts once per press.</summary>
public sealed record PanelBinding(Mode Mode) : ButtonBinding;

/// <summary>Taps Escape and closes the top panel, or opens the menu from Combat. Acts once per press.</summary>
public sealed record BackBinding : ButtonBinding;

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

    /// <summary>Every binding, base layer first, for showing in a legend.</summary>
    public IEnumerable<(GamepadButtons Button, ButtonBinding Binding, bool Chorded)> Entries
    {
        get
        {
            foreach ((GamepadButtons button, ButtonBinding binding) in baseLayer.OrderBy(entry => entry.Key))
            {
                yield return (button, binding, false);
            }

            foreach ((GamepadButtons button, ButtonBinding binding) in chordLayer.OrderBy(entry => entry.Key))
            {
                yield return (button, binding, true);
            }
        }
    }

    /// <summary>What one button is bound to with the trigger where it is, or null if nothing.</summary>
    public ButtonBinding? BindingFor(GamepadButtons button, double leftTrigger)
    {
        LayerFor(leftTrigger).TryGetValue(button, out ButtonBinding? binding);
        return binding;
    }

    /// <summary>The keys and mouse buttons that held bindings want down. Mode bindings act elsewhere.</summary>
    public (HashSet<KeyCode> Keys, HashSet<PointerButton> Buttons) Resolve(GamepadSnapshot snapshot)
    {
        HashSet<KeyCode> keys = new();
        HashSet<PointerButton> buttons = new();

        foreach ((GamepadButtons button, ButtonBinding binding) in LayerFor(snapshot.LeftTrigger))
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

                if (pointer.Modifier != KeyCode.None)
                {
                    keys.Add(pointer.Modifier);
                }
            }
        }

        return (keys, buttons);
    }

    private IReadOnlyDictionary<GamepadButtons, ButtonBinding> LayerFor(double leftTrigger)
    {
        if (leftTrigger >= triggerThreshold)
        {
            return chordLayer;
        }

        return baseLayer;
    }
}
