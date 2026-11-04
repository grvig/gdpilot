using System.Globalization;

namespace GDPilot.Core;

/// <summary>One profile from the configuration: the display it was calibrated on and its grids.</summary>
public sealed class ProfileConfig
{
    public int[] Resolution { get; init; } = Array.Empty<int>();
    public double UiScale { get; init; } = 1.0;
    public Dictionary<string, GridConfig> Grids { get; init; } = new();
    public Dictionary<string, SpotAreaConfig> Spots { get; init; } = new();
}

/// <summary>
/// Where a menu's spots are looked for, as the configuration writes it: a
/// rectangle of the screen, the look of the spots in it, and any fixed places
/// such as a button the finder cannot see, given as [x, y] pairs.
/// </summary>
public sealed class SpotAreaConfig
{
    public int Left { get; init; }
    public int Top { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public string Look { get; init; } = "";
    public int[][] Fixed { get; init; } = Array.Empty<int[]>();

    /// <summary>The area, or null when it is empty or its look is not one GDPilot knows.</summary>
    public SpotArea? ToArea()
    {
        List<ScreenPoint> places = Fixed.Where(pair => pair.Length == 2).Select(pair => new ScreenPoint(pair[0], pair[1])).ToList();

        switch (Look)
        {
            case "skillButton" when Width > 0 && Height > 0:
                return new SpotArea(Left, Top, Width, Height, SpotLook.SkillButton, places);
            case "devotionStar" when Width > 0 && Height > 0:
                return new SpotArea(Left, Top, Width, Height, SpotLook.DevotionStar, places);
            case "dialogueChoice" when Width > 0 && Height > 0:
                return new SpotArea(Left, Top, Width, Height, null, places, SpotKind.Replies);
            case "places" when places.Count > 0:
                return new SpotArea(Left, Top, Width, Height, null, places, SpotKind.Places);
            default:
                return null;
        }
    }
}

/// <summary>What a menu's spots are: buttons found by their look, a conversation's replies, or only the fixed places.</summary>
public enum SpotKind
{
    Buttons,
    Replies,
    Places,
}

/// <summary>A menu's spots: found inside the rectangle as its kind says, plus the fixed places.</summary>
public sealed record SpotArea(int Left, int Top, int Width, int Height, SpotLook? Look, IReadOnlyList<ScreenPoint> Fixed, SpotKind Kind = SpotKind.Buttons);

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
    /// <summary>The menus whose cursor snaps to spots rather than cells, by mode name.</summary>
    public IReadOnlyDictionary<string, SpotArea> Spots { get; init; } = new Dictionary<string, SpotArea>();

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

        Dictionary<string, SpotArea> spots = new();

        foreach ((string name, SpotAreaConfig area) in profile.Spots)
        {
            if (area.ToArea() is SpotArea ready)
            {
                spots[name] = ready;
            }
        }

        if (grids.Count == 0 && spots.Count == 0)
        {
            return new GridReadiness(false, "No grid in the active profile is calibrated yet.", none);
        }

        string message = $"Grid navigation ready for {string.Join(", ", grids.Keys.Order())}.";

        if (spots.Count > 0)
        {
            message += $" Snapping ready for {string.Join(", ", spots.Keys.Order())}.";
        }

        return new GridReadiness(true, message, grids) { Spots = spots };
    }
}
