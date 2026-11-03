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
}
