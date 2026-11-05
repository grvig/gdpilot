using GDPilot.Core;

namespace GDPilot.Integration.Tests;

/// <summary>
/// The dialogue reader against a real conversation with Harmond in Devil's
/// Crossing at 1920 by 1080, UI scale 0.5, cropped from (700, 600) on screen.
/// The box shows his name, one line of speech and one reply.
/// </summary>
public sealed class DialogueFixtureTests
{
    [Fact]
    public void TheOneReplyIsFoundOnItsLine()
    {
        Frame capture = Frame.Load(Path.Combine(AppContext.BaseDirectory, "screens", "dialogue.png"));

        List<ScreenPoint> choices = DialogueReader.Choices(capture.Pixel, 73, 0, 477, 335);

        ScreenPoint reply = Assert.Single(choices);
        Assert.InRange(reply.Y, 278, 294);
    }

    // The same conversation with the cursor on the reply, which the game bands in olive and lightens, cropped from (580, 760).
    [Fact]
    public void TheReplyUnderTheCursorIsStillFound()
    {
        Frame capture = Frame.Load(Path.Combine(AppContext.BaseDirectory, "screens", "dialogue-hover.png"));

        List<ScreenPoint> choices = DialogueReader.Choices(capture.Pixel, 193, 0, 477, 175);

        ScreenPoint reply = Assert.Single(choices);
        Assert.InRange(reply.Y, 118, 134);
    }

    // A longer conversation with Lancel the Knight, with two replies under its speech, cropped from (580, 640).
    [Fact]
    public void EachOfSeveralRepliesIsFoundInOrder()
    {
        Frame capture = Frame.Load(Path.Combine(AppContext.BaseDirectory, "screens", "dialogue-replies.png"));

        List<ScreenPoint> choices = DialogueReader.Choices(capture.Pixel, 193, 0, 477, 295);

        Assert.Equal(2, choices.Count);
        Assert.InRange(choices[0].Y, 225, 245);
        Assert.InRange(choices[1].Y, 255, 275);
    }

    // A hotbar slot's list, opened on the first slot and cropped from (660, 700), with nine rows.
    [Fact]
    public void EachRowOfASlotsListIsFound()
    {
        Frame capture = Frame.Load(Path.Combine(AppContext.BaseDirectory, "screens", "slotlist.png"));

        List<ScreenPoint> rows = SlotListReader.Rows(capture.Pixel, 0, 0, 340, 330);

        Assert.Equal(9, rows.Count);
        Assert.InRange(rows[^1].Y, 300, 315);
        Assert.Equal(34, rows[^1].Y - rows[^2].Y);
    }

    // The world map from a rift gate, cropped from (460, 180), with eight gates known and no others.
    [Fact]
    public void EveryRiftGateOnTheMapIsFoundAndNothingElse()
    {
        Frame capture = Frame.Load(Path.Combine(AppContext.BaseDirectory, "screens", "worldmap.png"));
        ScreenPoint[] gates =
        {
            new(524, 237), new(214, 230), new(324, 292), new(274, 141), new(162, 148), new(274, 95), new(570, 124), new(599, 60),
        };

        List<ScreenPoint> found = RiftGateFinder.Find(capture.Pixel, 22, 45, 968, 590);

        Assert.Equal(gates.Length, found.Count);

        foreach (ScreenPoint gate in gates)
        {
            Assert.Contains(found, spot => Math.Abs(spot.X - gate.X) <= 3 && Math.Abs(spot.Y - gate.Y) <= 3);
        }
    }
}
