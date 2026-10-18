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
