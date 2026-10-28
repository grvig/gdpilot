namespace GDPilot.Core;

/// <summary>"radial:hotbar": while held, the right stick points at a hotbar slot; letting go casts it.</summary>
public sealed record RadialBinding : ButtonBinding;

/// <summary>A small picture as 32-bit BGRA pixels, row after row from the top.</summary>
public sealed record Picture(byte[] Bgra, int Width, int Height);

/// <summary>What the overlay draws for an open wheel: each slot's label, and which is pointed at.</summary>
public sealed record WheelView(IReadOnlyList<string> Labels, int? Selected)
{
    /// <summary>Each slot's skill icon copied from the hotbar, or null where there is none.</summary>
    public IReadOnlyList<Picture?>? Pictures { get; init; }
}

/// <summary>
/// The skill wheel: slots laid round a circle, the first at the top and the
/// rest clockwise. The stick points at a slot; letting go of the wheel's button
/// casts whatever was last pointed at, and releasing the stick first changes
/// nothing, so a flick and release is all a cast takes.
/// </summary>
public sealed class RadialMenu
{
    /// <summary>How far the stick must lean before it points at a slot rather than the middle.</summary>
    public const double PointFrom = 0.5;

    public RadialMenu(IReadOnlyList<KeyCode> slots)
    {
        if (slots.Count == 0)
        {
            throw new ArgumentException("A wheel needs at least one slot.", nameof(slots));
        }

        Slots = slots;
    }

    public IReadOnlyList<KeyCode> Slots { get; }

    public bool Open { get; private set; }

    /// <summary>The slot last pointed at while open, or null when none has been.</summary>
    public int? Selected { get; private set; }

    /// <summary>
    /// One tick of the wheel's button and the stick. Returns the key of the slot
    /// to cast when the button is let go with a slot selected, otherwise null.
    /// </summary>
    public KeyCode? Update(bool held, StickPosition stick)
    {
        if (held)
        {
            if (!Open)
            {
                Open = true;
                Selected = null;
            }

            if (stick.Magnitude >= PointFrom)
            {
                Selected = SlotAt(stick);
            }

            return null;
        }

        if (!Open)
        {
            return null;
        }

        Open = false;

        if (Selected is int chosen)
        {
            Selected = null;
            return Slots[chosen];
        }

        return null;
    }

    /// <summary>The wheel as the overlay should show it, or null while closed. Number keys read as their digit.</summary>
    public WheelView? View()
    {
        if (!Open)
        {
            return null;
        }

        List<string> labels = new();

        foreach (KeyCode key in Slots)
        {
            if (key is >= KeyCode.D0 and <= KeyCode.D9)
            {
                labels.Add(((int)(key - KeyCode.D0)).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            else
            {
                labels.Add(key.ToString());
            }
        }

        return new WheelView(labels, Selected);
    }

    /// <summary>
    /// Where a key's slot sits on Grim Dawn's hotbar, counting its twelve cells
    /// from the left: keys 1 to 5, then the two mouse-button slots, then 6 to 0.
    /// Null for a key the hotbar does not show.
    /// </summary>
    public static int? HotbarColumn(KeyCode key)
    {
        if (key is >= KeyCode.D1 and <= KeyCode.D5)
        {
            return key - KeyCode.D1;
        }

        if (key is >= KeyCode.D6 and <= KeyCode.D9)
        {
            return key - KeyCode.D6 + 7;
        }

        if (key == KeyCode.D0)
        {
            return 11;
        }

        return null;
    }

    /// <summary>Which slot a stick direction points at: the first straight up, then clockwise.</summary>
    public int SlotAt(StickPosition stick)
    {
        double clockwiseFromUp = Math.Atan2(stick.X, stick.Y);

        if (clockwiseFromUp < 0)
        {
            clockwiseFromUp += 2 * Math.PI;
        }

        double width = 2 * Math.PI / Slots.Count;
        return (int)Math.Floor((clockwiseFromUp + width / 2) / width) % Slots.Count;
    }
}
