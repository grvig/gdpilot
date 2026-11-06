namespace GDPilot.Core;

/// <summary>
/// How large the game draws its interface: the sum GraphicsEngine::GetUIScaleFactor
/// does, read from its code on 2026-11-07 and checked against captures at several
/// displays. It grows with the window's area up to a point, then the setting takes
/// over, and the setting below a half shrinks it whatever the window.
/// </summary>
public static class UiFactor
{
    /// <summary>The pixels at which the window stops counting: 2400 by 1920.</summary>
    private const double FullArea = 4608000;

    public static double Of(int width, int height, double uiScale)
    {
        double setting = (uiScale - 0.5) * 2;

        if (setting < 0)
        {
            return setting * 0.3 + 1;
        }

        double area = Math.Clamp(width * (double)height / FullArea, 0, 1);

        if (area > 0.75)
        {
            return setting + 1;
        }

        double byArea = area * 0.5 + 0.7;

        // Near enough to one is drawn at one, so a common display is not resampled.
        if (Math.Abs(1 - byArea) <= 0.1)
        {
            byArea = 1;
        }

        return (area * 0.7 + 0.1) * setting + byArea;
    }
}
