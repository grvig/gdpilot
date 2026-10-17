namespace GDPilot.Core;

/// <summary>
/// Turns "what should be held this tick" into presses and releases. Every
/// mapping describes held state; only this class knows what changed.
/// </summary>
public sealed class HeldInputDiffer
{
    private readonly HashSet<KeyCode> heldKeys = new();
    private readonly HashSet<PointerButton> heldButtons = new();

    public List<InputIntent> Update(IReadOnlySet<KeyCode> wantedKeys, IReadOnlySet<PointerButton> wantedButtons)
    {
        List<InputIntent> intents = new();

        // Releases go first. Swinging the stick from up-left to up-right would
        // otherwise hold A, W and D together for a moment.
        foreach (KeyCode key in heldKeys.Except(wantedKeys).Order())
        {
            intents.Add(new KeyUp(key));
        }

        foreach (PointerButton button in heldButtons.Except(wantedButtons).Order())
        {
            intents.Add(new PointerUp(button));
        }

        foreach (KeyCode key in wantedKeys.Except(heldKeys).Order())
        {
            intents.Add(new KeyDown(key));
        }

        foreach (PointerButton button in wantedButtons.Except(heldButtons).Order())
        {
            intents.Add(new PointerDown(button));
        }

        heldKeys.Clear();
        heldKeys.UnionWith(wantedKeys);
        heldButtons.Clear();
        heldButtons.UnionWith(wantedButtons);
        return intents;
    }
}
