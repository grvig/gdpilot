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

    /// <summary>How fast the right stick's up and down zooms the third-person camera, in zoom range per second.</summary>
    public double ZoomSpeed { get; init; } = 0.6;

    /// <summary>
    /// How fast the third-person camera turns toward a sideways walk, in radians
    /// per second, while the right stick is left alone. Zero turns it off.
    /// </summary>
    public double FollowSpeed { get; init; } = 0;

    /// <summary>Whether the pad rumbles for heavy blows and low health, which needs the in-game part's health reports.</summary>
    public bool Rumble { get; init; } = true;

    /// <summary>
    /// The keys on the skill wheel, the first at the top and the rest clockwise:
    /// by default Grim Dawn's hotbar, 1 to 0.
    /// </summary>
    public List<string> RadialSlots { get; init; } = new() { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };

    /// <summary>
    /// How far down the game window the cursor waits in third person, from 0 at
    /// the top to 1 at the bottom. Just above the character puts attacks ahead of it.
    /// </summary>
    public double AimHeight { get; init; } = 0.38;

    /// <summary>
    /// How near an enemy must be, in world units, for lock-on to take it. A lock
    /// breaks a third further out, so it does not flicker at the edge.
    /// </summary>
    public double LockRange { get; init; } = 25;

    /// <summary>
    /// Whether, in third person without a lock, the cursor drifts onto an enemy
    /// close to where the camera points, so a rough swing lands.
    /// </summary>
    public bool SoftAim { get; init; } = true;

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
