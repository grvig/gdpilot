using System.Globalization;

namespace GDPilot.Core;

/// <summary>
/// Checks the game's own keybindings against the keys GDPilot presses. The game
/// keeps them in keybindings.txt, one line a function, "line: primary secondary",
/// each key a scan code. Which line is which function was read on 2026-11-08 by
/// matching the game's keybinding screen against its file, and does not change
/// with the keys chosen.
/// </summary>
public static class GameKeys
{
    /// <summary>Each function GDPilot can press, by its line in keybindings.txt and its name on the game's keybinding screen.</summary>
    private static readonly Dictionary<string, (int Line, string Function)> Functions = new()
    {
        ["Forward"] = (34, "Forward"),
        ["Backward"] = (2, "Backward"),
        ["Left"] = (51, "Left"),
        ["Right"] = (32, "Right"),
        [nameof(Mode.Inventory)] = (1, "Character Window"),
        [nameof(Mode.Skills)] = (6, "Skill Window"),
        [nameof(Mode.Journal)] = (3, "Codex Window"),
        [nameof(Mode.WorldMap)] = (4, "Map Window"),
        [nameof(Mode.LootFilter)] = (56, "Loot Filter Window"),
        [nameof(Mode.Factions)] = (52, "Factions Window"),
        [nameof(Mode.Achievements)] = (53, "Achievements Window"),
        ["Rift"] = (33, "Personal Riftgate"),
    };

    /// <summary>The first quickbar slot's line; the other nine follow it, for the keys 1 to 9 and then 0.</summary>
    private const int FirstQuickbarLine = 9;

    /// <summary>What GDPilot presses for each function: the four movement keys, the panel keys, the quickbar and any rift binding.</summary>
    public static IReadOnlyList<(string Function, int Line, KeyCode Key)> Wanted(GDPilotConfig config)
    {
        List<(string Function, int Line, KeyCode Key)> wanted = new();
        DirectionKeys movement = config.Mapping().Movement;

        Add(wanted, "Forward", movement.Up);
        Add(wanted, "Backward", movement.Down);
        Add(wanted, "Left", movement.Left);
        Add(wanted, "Right", movement.Right);

        foreach ((Mode mode, KeyCode key) in config.PanelKeys())
        {
            Add(wanted, mode.ToString(), key);
        }

        for (int slot = 1; slot <= 10; slot++)
        {
            KeyCode key = KeyCode.D0 + slot % 10;
            wanted.Add(($"Quickbar Slot {slot:00}", FirstQuickbarLine + slot - 1, key));
        }

        foreach (string mode in config.Bindings.Keys)
        {
            foreach ((GamepadButtons _, ButtonBinding binding, bool _) in config.ButtonsFor(mode).Entries)
            {
                if (binding is RiftBinding rift && !wanted.Any(entry => entry.Function == Functions["Rift"].Function))
                {
                    Add(wanted, "Rift", rift.Key);
                }
            }
        }

        return wanted;
    }

    /// <summary>Each function whose key in the game is not the one GDPilot presses for it, said as what to set.</summary>
    public static IReadOnlyList<string> Mismatches(string keybindings, IReadOnlyList<(string Function, int Line, KeyCode Key)> wanted)
    {
        Dictionary<int, (int Primary, int Secondary)> lines = Parse(keybindings);
        List<string> mismatches = new();

        foreach ((string function, int line, KeyCode key) in wanted)
        {
            if (ScanCode(key) is not int code || !lines.TryGetValue(line, out (int Primary, int Secondary) bound))
            {
                continue;
            }

            if (bound.Primary != code && bound.Secondary != code)
            {
                mismatches.Add($"{function} on {Name(key)}");
            }
        }

        return mismatches;
    }

    private static void Add(List<(string Function, int Line, KeyCode Key)> wanted, string name, KeyCode key)
    {
        if (Functions.TryGetValue(name, out (int Line, string Function) known))
        {
            wanted.Add((known.Function, known.Line, key));
        }
    }

    private static Dictionary<int, (int Primary, int Secondary)> Parse(string text)
    {
        Dictionary<int, (int Primary, int Secondary)> lines = new();

        foreach (string line in text.Split('\n'))
        {
            string[] halves = line.Split(':', 2);

            if (halves.Length != 2 || !int.TryParse(halves[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
            {
                continue;
            }

            string[] codes = halves[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int primary = 0;
            int secondary = 0;

            if (codes.Length > 0)
            {
                int.TryParse(codes[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out primary);
            }

            if (codes.Length > 1)
            {
                int.TryParse(codes[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out secondary);
            }

            lines[number] = (primary, secondary);
        }

        return lines;
    }

    /// <summary>The scan code the game files a key under, for the keys GDPilot presses; null for any other.</summary>
    public static int? ScanCode(KeyCode key)
    {
        const string TopRow = "QWERTYUIOP";
        const string MiddleRow = "ASDFGHJKL";
        const string BottomRow = "ZXCVBNM";

        if (key >= KeyCode.D1 && key <= KeyCode.D9)
        {
            return 2 + (key - KeyCode.D1);
        }

        switch (key)
        {
            case KeyCode.D0:
                return 11;
            case KeyCode.Tab:
                return 15;
            case KeyCode.Space:
                return 57;
        }

        string name = key.ToString();

        if (name.Length != 1)
        {
            return null;
        }

        if (TopRow.Contains(name[0]))
        {
            return 16 + TopRow.IndexOf(name[0]);
        }

        if (MiddleRow.Contains(name[0]))
        {
            return 30 + MiddleRow.IndexOf(name[0]);
        }

        if (BottomRow.Contains(name[0]))
        {
            return 44 + BottomRow.IndexOf(name[0]);
        }

        return null;
    }

    private static string Name(KeyCode key)
    {
        if (key >= KeyCode.D0 && key <= KeyCode.D9)
        {
            return ((int)(key - KeyCode.D0)).ToString(CultureInfo.InvariantCulture);
        }

        return key.ToString();
    }
}
