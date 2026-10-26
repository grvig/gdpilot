namespace GDPilot.Core;

/// <summary>The "input" section of the configuration, with section 7's values as defaults.</summary>
public sealed class InputConfig
{
    public int PollHz { get; init; } = 250;
    public DeadzoneConfig Deadzone { get; init; } = new();

    /// <summary>How far the shaped left stick must lean before movement keys are held.</summary>
    public double WalkThreshold { get; init; } = 0.25;

    /// <summary>How far the left trigger must be pulled to switch to the chord layer.</summary>
    public double ChordTrigger { get; init; } = 0.5;

    public CursorConfig Cursor { get; init; } = new();

    /// <summary>How fast the right stick turns the third-person camera at full tilt, in radians per second.</summary>
    public double LookSpeed { get; init; } = 2.5;

    /// <summary>
    /// Whether grid presses read the screen and jump between items. Off, they
    /// step one cell at a time, which never depends on what the screen shows.
    /// </summary>
    public bool JumpToItems { get; init; } = true;
}

public sealed class DeadzoneConfig
{
    public double Left { get; init; } = 0.22;
    public double Right { get; init; } = 0.18;
}

public sealed class CursorConfig
{
    public double BaseSpeed { get; init; } = 900;
    public double Exponent { get; init; } = 2.0;
    public double MaxSpeed { get; init; } = 2600;
}
