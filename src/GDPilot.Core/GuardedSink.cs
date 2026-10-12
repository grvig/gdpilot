namespace GDPilot.Core;

/// <summary>
/// Stands in front of the real sink and remembers everything it has pressed but
/// not released. A synthetic key left down stays down for the whole operating
/// system after this process dies, so every exit path ends in <see cref="ReleaseAll"/>.
/// </summary>
public sealed class GuardedSink : IInputSink
{
    private readonly IInputSink inner;
    private readonly object gate = new();
    private readonly HashSet<KeyCode> heldKeys = new();
    private readonly HashSet<PointerButton> heldButtons = new();

    public GuardedSink(IInputSink inner)
    {
        this.inner = inner;
    }

    public void Send(InputIntent intent)
    {
        // Locked because releases arrive from other threads, such as a watchdog,
        // and must never interleave with a press.
        lock (gate)
        {
            inner.Send(intent);
            Track(intent);
        }
    }

    public void ReleaseAll()
    {
        lock (gate)
        {
            ReleaseHeld();
        }
    }

    private void Track(InputIntent intent)
    {
        if (intent is KeyDown down)
        {
            heldKeys.Add(down.Key);
        }
        else if (intent is KeyUp up)
        {
            heldKeys.Remove(up.Key);
        }
        else if (intent is PointerDown pressed)
        {
            heldButtons.Add(pressed.Button);
        }
        else if (intent is PointerUp released)
        {
            heldButtons.Remove(released.Button);
        }
    }

    private void ReleaseHeld()
    {
        List<InputIntent> releases = heldKeys.Select(key => (InputIntent)new KeyUp(key)).ToList();
        releases.AddRange(heldButtons.Select(button => new PointerUp(button)));
        heldKeys.Clear();
        heldButtons.Clear();

        // One failed release must not strand the rest, so every one is attempted
        // and the first failure is reported only afterwards.
        Exception? firstFailure = null;

        foreach (InputIntent release in releases)
        {
            try
            {
                inner.Send(release);
            }
            catch (Exception failure)
            {
                firstFailure ??= failure;
            }
        }

        if (firstFailure is not null)
        {
            throw new InvalidOperationException("Not every held input could be released.", firstFailure);
        }
    }
}
