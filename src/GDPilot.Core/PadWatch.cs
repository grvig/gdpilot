namespace GDPilot.Core;

public enum PadChange
{
    None,
    Lost,
    Found,
}

/// <summary>
/// Says when the pad goes or comes back, after the first look, which startup
/// has already reported. A pad that comes back as another kind of pad, or
/// another pad of the same kind, is told apart by its name.
/// </summary>
public sealed class PadWatch
{
    private bool? connected;

    /// <summary>The name of the pad last seen, kept while it is gone so its return can be recognised.</summary>
    public string? LastName { get; private set; }

    public PadChange Changed(bool now, string? name)
    {
        bool? before = connected;
        connected = now;

        if (before is null || before == now)
        {
            if (now)
            {
                LastName = name;
            }

            return PadChange.None;
        }

        if (now)
        {
            return PadChange.Found;
        }

        return PadChange.Lost;
    }

    /// <summary>After a pad is found: whether it is the one that went, whose rest positions still hold.</summary>
    public bool SameAsBefore(string? name)
    {
        bool same = name is not null && name == LastName;
        LastName = name;
        return same;
    }
}
