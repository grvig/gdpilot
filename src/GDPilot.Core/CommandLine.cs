using System.Globalization;

namespace GDPilot.Core;

/// <summary>
/// What GDPilot.exe accepts. An option it does not know is refused by name,
/// since starting the whole program on a mistyped one would take over the pad
/// and the game when the person meant only to check something.
/// </summary>
public static class CommandLine
{
    public static readonly IReadOnlyList<string> Options = new[]
    {
        "--at", "--calibrate", "--capture", "--check-game", "--counts", "--detect", "--dry-run", "--install-loader", "--load-in-game",
        "--occupancy", "--remove-loader", "--replay", "--rumble-test", "--seconds", "--setup", "--spots", "--targets", "--to",
    };

    /// <summary>The first option given that GDPilot does not know, or null when all are known.</summary>
    public static string? Unknown(IEnumerable<string> args)
    {
        return args.FirstOrDefault(arg => arg.StartsWith("--", StringComparison.Ordinal) && !Options.Contains(arg));
    }

    /// <summary>The number after an option, null when the option is not given, or a reason it cannot be read.</summary>
    public static (double? Value, string? Problem) Number(IReadOnlyList<string> args, string option)
    {
        int at = args.ToList().IndexOf(option);

        if (at < 0)
        {
            return (null, null);
        }

        if (at + 1 >= args.Count || !double.TryParse(args[at + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || value <= 0)
        {
            return (null, $"{option} needs a number of seconds above zero after it.");
        }

        return (value, null);
    }

    /// <summary>The whole numbers after an option, null when it is not given, or a reason they cannot be read.</summary>
    public static (int[]? Values, string? Problem) Whole(IReadOnlyList<string> args, string option, int count)
    {
        int at = args.ToList().IndexOf(option);

        if (at < 0)
        {
            return (null, null);
        }

        int[] values = new int[count];

        for (int index = 0; index < count; index++)
        {
            if (at + 1 + index >= args.Count || !int.TryParse(args[at + 1 + index], NumberStyles.Integer, CultureInfo.InvariantCulture, out values[index]))
            {
                return (null, $"{option} needs {count} whole numbers after it.");
            }
        }

        return (values, null);
    }
}
