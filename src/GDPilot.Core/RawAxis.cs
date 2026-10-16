namespace GDPilot.Core;

/// <summary>
/// Converts raw signed 16-bit axis readings, the range SDL and XInput both use,
/// into the normalised values the rest of the pipeline works in.
/// </summary>
public static class RawAxis
{
    private const double Full = 32767.0;

    /// <summary>
    /// A stick from its two raw axes. Hardware reports down as positive Y; the
    /// pipeline uses up as positive, so Y is flipped here and nowhere else.
    /// </summary>
    public static StickPosition Stick(short rawX, short rawY)
    {
        return new StickPosition(Clamp(rawX / Full), Clamp(-rawY / Full));
    }

    /// <summary>A trigger from 0 at rest to 1 fully pulled.</summary>
    public static double Trigger(short raw)
    {
        double value = raw / Full;

        // Some pads rest a trigger slightly below zero.
        if (value < 0)
        {
            return 0;
        }

        return value;
    }

    // The negative range is one step longer than the positive, so -32768 alone
    // would come out a hair past -1.
    private static double Clamp(double value)
    {
        return Math.Clamp(value, -1, 1);
    }
}
