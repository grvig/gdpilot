using System.Text.Json;

namespace GDPilot.Core;

/// <summary>
/// Says when the settings file has been saved, so what it changes takes hold at
/// once. An editor can write a file in more than one go, so a change counts once
/// the file has stood still a moment. A file that does not read keeps the
/// settings as they were, and is not read again until it is saved again.
/// </summary>
public sealed class SettingsWatch
{
    public static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(300);

    private readonly Func<DateTime?> written;
    private readonly Func<GDPilotConfig> load;
    private DateTime? applied;
    private DateTime? seen;
    private TimeSpan seenAt;

    /// <param name="written">When the file was last saved, or null when it is not there.</param>
    /// <param name="load">Reads and checks the file.</param>
    public SettingsWatch(Func<DateTime?> written, Func<GDPilotConfig> load)
    {
        this.written = written;
        this.load = load;
        applied = written();
    }

    /// <summary>Why the last save was not taken, or null when it was.</summary>
    public string? Problem { get; private set; }

    /// <summary>The settings as just saved, or null when nothing new has settled.</summary>
    public GDPilotConfig? Changed(TimeSpan now)
    {
        DateTime? stamp = written();

        if (stamp is null || stamp == applied)
        {
            seen = null;
            return null;
        }

        if (stamp != seen)
        {
            seen = stamp;
            seenAt = now;
            return null;
        }

        if (now - seenAt < Settle)
        {
            return null;
        }

        seen = null;

        try
        {
            GDPilotConfig fresh = load();
            applied = stamp;
            Problem = null;
            return fresh;
        }
        catch (IOException)
        {
            // Still being written by something holding it open; the next look will do.
            return null;
        }
        catch (FormatException problem)
        {
            applied = stamp;
            Problem = problem.Message;
            return null;
        }
        catch (JsonException problem)
        {
            applied = stamp;
            Problem = problem.Message;
            return null;
        }
    }
}
