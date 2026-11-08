namespace GDPilot.Core;

/// <summary>
/// Buttons as a bitfield, so a trace fixture can record a whole pad state as one
/// number and a chord is a single mask comparison.
/// </summary>
[Flags]
public enum GamepadButtons : uint
{
    None = 0,
    A = 1 << 0,
    B = 1 << 1,
    X = 1 << 2,
    Y = 1 << 3,
    LeftBumper = 1 << 4,
    RightBumper = 1 << 5,
    Back = 1 << 6,
    Start = 1 << 7,
    LeftStick = 1 << 8,
    RightStick = 1 << 9,
    DPadUp = 1 << 10,
    DPadDown = 1 << 11,
    DPadLeft = 1 << 12,
    DPadRight = 1 << 13,
    Guide = 1 << 14,
}

/// <summary>
/// A stick deflection from -1 to 1 on each axis. Positive Y is up, the maths
/// convention, so a source whose hardware reports down as positive flips it.
/// </summary>
public readonly record struct StickPosition(double X, double Y)
{
    public static readonly StickPosition Centre = new(0, 0);

    public double Magnitude
    {
        get
        {
            return Math.Sqrt(X * X + Y * Y);
        }
    }
}

/// <summary>One tick's complete pad state. Triggers run from 0 at rest to 1 fully pulled.</summary>
public readonly record struct GamepadSnapshot(
    GamepadButtons Buttons,
    StickPosition LeftStick,
    StickPosition RightStick,
    double LeftTrigger,
    double RightTrigger)
{
    /// <summary>No buttons, both sticks centred, both triggers released.</summary>
    public static readonly GamepadSnapshot Rest = new(GamepadButtons.None, StickPosition.Centre, StickPosition.Centre, 0, 0);

    /// <summary>The pad has gone, so this rest is not a person letting go of anything.</summary>
    public bool Gone { get; init; }

    public bool IsPressed(GamepadButtons buttons)
    {
        return (Buttons & buttons) == buttons;
    }
}

/// <summary>
/// Where snapshots come from. Real hardware and a replayed trace both sit behind
/// this, which is what lets the whole pipeline run in a test with no controller.
/// </summary>
public interface IGamepadSource
{
    bool IsConnected { get; }

    GamepadSnapshot Read();
}
