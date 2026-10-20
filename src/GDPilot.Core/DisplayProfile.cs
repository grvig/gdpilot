using System.Globalization;

namespace GDPilot.Core;

/// <summary>One profile from the configuration: the display it was calibrated on and its grids.</summary>
public sealed class ProfileConfig
{
    public int[] Resolution { get; init; } = Array.Empty<int>();
    public double UiScale { get; init; } = 1.0;
    public Dictionary<string, GridConfig> Grids { get; init; } = new();
}

/// <summary>A grid as the configuration writes it, zeros meaning not yet calibrated.</summary>
public sealed class GridConfig
{
    public double OriginX { get; init; }
    public double OriginY { get; init; }
    public double PitchX { get; init; }
    public double PitchY { get; init; }
    public int Columns { get; init; }
    public int Rows { get; init; }

    public GridSpec ToSpec()
    {
        return new GridSpec(OriginX, OriginY, PitchX, PitchY, Columns, Rows);
    }
}

/// <summary>The display the game is actually running at, read at startup.</summary>
public readonly record struct LiveDisplay(int Width, int Height, double UiScale)
{
    /// <summary>
    /// Profile names pair resolution with UI scale, because menu windows in this
    /// game version scale with the UI setting: either one changing moves every cell.
    /// </summary>
    public string ProfileKey => string.Create(CultureInfo.InvariantCulture, $"{Width}x{Height}@{UiScale:0.0##}");
}

/// <summary>The outcome of comparing the live display with the active profile.</summary>
public sealed record GridReadiness(bool Ready, string Message, IReadOnlyDictionary<string, GridSpec> Grids)
{
    /// <summary>
    /// Grid navigation runs only when the active profile was calibrated on the
    /// display the game is using now. Anything else would click the wrong pixels
    /// without saying so, so it is refused with a reason instead.
    /// </summary>
    public static GridReadiness Check(GDPilotConfig config, LiveDisplay live)
    {
        Dictionary<string, GridSpec> none = new();

        if (!config.Profiles.TryGetValue(config.ActiveProfile, out ProfileConfig? profile))
        {
            return new GridReadiness(false, $"No profile named \"{config.ActiveProfile}\". Calibrate one for {live.ProfileKey}.", none);
        }

        bool sameResolution = profile.Resolution.Length == 2 && profile.Resolution[0] == live.Width && profile.Resolution[1] == live.Height;
        bool sameScale = Math.Abs(profile.UiScale - live.UiScale) < 0.001;

        if (!sameResolution || !sameScale)
        {
            string calibrated = string.Join("x", profile.Resolution);
            return new GridReadiness(false, $"Grids were calibrated at {calibrated}, UI scale {profile.UiScale}, but the game runs {live.Width}x{live.Height}, UI scale {live.UiScale}. Recalibrate.", none);
        }

        Dictionary<string, GridSpec> grids = new();

        foreach ((string name, GridConfig grid) in profile.Grids)
        {
            GridSpec spec = grid.ToSpec();

            if (spec.IsCalibrated)
            {
                grids[name] = spec;
            }
        }

        if (grids.Count == 0)
        {
            return new GridReadiness(false, "No grid in the active profile is calibrated yet.", none);
        }

        return new GridReadiness(true, $"Grid navigation ready for {string.Join(", ", grids.Keys.Order())}.", grids);
    }
}
