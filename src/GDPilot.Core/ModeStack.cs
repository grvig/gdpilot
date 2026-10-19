namespace GDPilot.Core;

/// <summary>The game UI states from section 7.</summary>
public enum Mode
{
    Suspended,
    Combat,
    Inventory,
    Stash,
    Character,
    Skills,
    Devotion,
    WorldMap,
    Vendor,
    Dialog,
    Menu,
}

/// <summary>
/// Which panels the tool believes are open, with Combat always at the bottom.
/// Suspension is tracked separately by <see cref="Suspension"/>, because being
/// suspended does not close any panel in the game.
/// </summary>
public sealed class ModeStack
{
    private readonly List<Mode> stack = new() { Mode.Combat };

    public Mode Current => stack[^1];

    /// <summary>How many panels are open above Combat, which is how many escapes close them all.</summary>
    public int Depth => stack.Count - 1;

    /// <summary>
    /// What pressing a panel's own key does in the game: it opens the panel,
    /// or closes it if it is already open. A panel open further down is closed
    /// where it sits, so the stack never holds the same panel twice.
    /// </summary>
    public void Toggle(Mode mode)
    {
        if (mode is Mode.Combat or Mode.Suspended)
        {
            throw new ArgumentException($"{mode} is not a panel and cannot be opened.", nameof(mode));
        }

        int index = stack.LastIndexOf(mode);

        if (index > 0)
        {
            stack.RemoveAt(index);
            return;
        }

        stack.Add(mode);
    }

    /// <summary>What Escape does: closes the top panel. False when nothing is open.</summary>
    public bool Pop()
    {
        if (stack.Count == 1)
        {
            return false;
        }

        stack.RemoveAt(stack.Count - 1);
        return true;
    }

    public void ClearToCombat()
    {
        stack.RemoveRange(1, stack.Count - 1);
    }
}
