namespace GDPilot.Core;

public enum PointerButton
{
    Left,
    Right,
}

/// <summary>
/// Something to inject, described without saying how. Records compare by value,
/// so a test asserts an exact output sequence with a plain equality check.
/// </summary>
public abstract record InputIntent;

public sealed record KeyDown(KeyCode Key) : InputIntent;

public sealed record KeyUp(KeyCode Key) : InputIntent;

/// <summary>A relative pointer move, in pixels.</summary>
public sealed record PointerMove(int Dx, int Dy) : InputIntent;

/// <summary>An absolute pointer move to a physical screen pixel.</summary>
public sealed record PointerMoveTo(int X, int Y) : InputIntent;

public sealed record PointerDown(PointerButton Button) : InputIntent;

public sealed record PointerUp(PointerButton Button) : InputIntent;

/// <summary>A command for the in-game part rather than input for Windows; the host routes it down the pipe.</summary>
public sealed record GameCommandIntent(GameCommand Command) : InputIntent;

/// <summary>Where intents go. The real one injects into Windows, the recording one into a list.</summary>
public interface IInputSink
{
    void Send(InputIntent intent);
}

/// <summary>Keeps every intent it receives, in order, for a test to assert against.</summary>
public sealed class RecordingSink : IInputSink
{
    public List<InputIntent> Sent { get; } = new();

    public void Send(InputIntent intent)
    {
        Sent.Add(intent);
    }
}
