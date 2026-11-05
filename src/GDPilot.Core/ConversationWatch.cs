namespace GDPilot.Core;

/// <summary>
/// Decides whether the game's talk report still means a conversation. The
/// player's talk state lasts until the character next moves, long after a
/// goodbye has closed the box, so a report of "Dialog" counts only while the
/// box is on screen. The screen is read a few times a second and only while
/// the report stands, and it takes two readings in a row to believe the box
/// has gone, so a reading made as the box changes step cannot end it.
/// </summary>
public sealed class ConversationWatch
{
    public static readonly TimeSpan ReadEvery = TimeSpan.FromMilliseconds(120);

    private const string Dialog = "Dialog";

    private readonly Func<bool> boxShowing;
    private TimeSpan sinceRead = ReadEvery;
    private bool showing;
    private int misses;

    public ConversationWatch(Func<bool> boxShowing)
    {
        this.boxShowing = boxShowing;
    }

    /// <summary>The windows the game reports, without a conversation whose box is not showing.</summary>
    public IReadOnlyList<string> Filter(IReadOnlyList<string> open, TimeSpan elapsed)
    {
        if (!open.Contains(Dialog))
        {
            sinceRead = ReadEvery;
            showing = false;
            misses = 0;
            return open;
        }

        sinceRead += elapsed;

        if (sinceRead >= ReadEvery)
        {
            sinceRead = TimeSpan.Zero;
            Read();
        }

        if (showing)
        {
            return open;
        }

        return open.Where(name => name != Dialog).ToList();
    }

    private void Read()
    {
        if (boxShowing())
        {
            showing = true;
            misses = 0;
            return;
        }

        misses++;

        if (misses >= 2)
        {
            showing = false;
        }
    }
}
