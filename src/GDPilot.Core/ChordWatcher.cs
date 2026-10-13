namespace GDPilot.Core;

/// <summary>
/// Follows raw key events and says when a chord completes. Modifier state is
/// tracked from the events themselves rather than asked of the operating
/// system, because a later hook may swallow an event before Windows records it.
/// </summary>
public sealed class ChordWatcher
{
    // Windows reports Pause as Cancel while Ctrl is held, before any hook sees it.
    private const uint VirtualKeyCancel = 0x03;

    private readonly KeyChord chord;
    private KeyModifiers held;

    public ChordWatcher(KeyChord chord)
    {
        this.chord = chord;
    }

    /// <summary>True on the key press that completes the chord.</summary>
    public bool Observe(uint virtualKey, bool isDown)
    {
        KeyModifiers modifier = ModifierFor(virtualKey);

        if (isDown)
        {
            held |= modifier;
        }
        else
        {
            held &= ~modifier;
        }

        return isDown && chord.Matches(KeyFor(virtualKey), held);
    }

    private static KeyModifiers ModifierFor(uint virtualKey)
    {
        switch (virtualKey)
        {
            case 0x10:
            case 0xA0:
            case 0xA1:
                return KeyModifiers.Shift;
            case 0x11:
            case 0xA2:
            case 0xA3:
                return KeyModifiers.Control;
            case 0x12:
            case 0xA4:
            case 0xA5:
                return KeyModifiers.Alt;
            default:
                return KeyModifiers.None;
        }
    }

    private static KeyCode KeyFor(uint virtualKey)
    {
        if (virtualKey == VirtualKeyCancel)
        {
            return KeyCode.Pause;
        }

        return (KeyCode)virtualKey;
    }
}
