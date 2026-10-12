using GDPilot.Core;

namespace GDPilot.Output;

/// <summary>Turns intents into real operating system input events.</summary>
public sealed class SendInputSink : IInputSink
{
    private readonly KeySender keys;
    private readonly MouseSender mouse = new();

    /// <summary>
    /// Scan codes by default. The feasibility probe on 2026-10-12 found Grim Dawn
    /// ignores virtual key events in play and moves only on scan codes.
    /// </summary>
    public SendInputSink()
        : this(KeyInjectionMode.ScanCode)
    {
    }

    public SendInputSink(KeyInjectionMode mode)
    {
        keys = new KeySender(mode);
    }

    public void Send(InputIntent intent)
    {
        switch (intent)
        {
            case KeyDown down:
                keys.Down(down.Key);
                break;
            case KeyUp up:
                keys.Up(up.Key);
                break;
            case PointerMove move:
                mouse.MoveRelative(move.Dx, move.Dy);
                break;
            case PointerMoveTo moveTo:
                mouse.MoveAbsolute(moveTo.X, moveTo.Y);
                break;
            case PointerDown pressed:
                mouse.Down(ButtonFor(pressed.Button));
                break;
            case PointerUp released:
                mouse.Up(ButtonFor(released.Button));
                break;
            default:
                throw new ArgumentException($"No injection exists for {intent.GetType().Name}.", nameof(intent));
        }
    }

    private static MouseButton ButtonFor(PointerButton button)
    {
        if (button == PointerButton.Left)
        {
            return MouseButton.Left;
        }

        return MouseButton.Right;
    }
}
