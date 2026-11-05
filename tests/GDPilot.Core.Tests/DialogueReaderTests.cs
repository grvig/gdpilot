using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class DialogueReaderTests
{
    private const int Width = 480;
    private const int Height = 300;

    /// <summary>
    /// A town at brightness 60 with a box from row 150 down, holding the speaker's
    /// name, a line of speech, two replies and a thin gilded trim; a gold sign
    /// hangs in the town above it, not lined up with the text.
    /// </summary>
    private static (byte, byte, byte) Scene(int x, int y)
    {
        bool text = x >= 10 && x < 200 && x % 3 == 0;

        if (y < 150)
        {
            if (x >= 90 && x < 200 && x % 3 == 0 && y >= 50 && y < 65)
            {
                return (220, 180, 60);
            }

            return (60, 60, 60);
        }

        bool gold = (y >= 170 && y < 181) || (y >= 230 && y < 241) || (y >= 255 && y < 266) || (y >= 290 && y < 294);
        bool white = y >= 200 && y < 211;

        if (text && gold)
        {
            return (220, 180, 60);
        }

        if (text && white)
        {
            return (230, 230, 230);
        }

        return (10, 9, 8);
    }

    [Fact]
    public void TheRepliesAreTheGoldLinesAfterTheSpeakersName()
    {
        List<ScreenPoint> choices = DialogueReader.Choices(Scene, 0, 0, Width, Height);

        Assert.Equal(new[] { new ScreenPoint(60, 235), new ScreenPoint(60, 260) }, choices);
    }

    [Fact]
    public void WithoutABoxThereIsNothingShowing()
    {
        static (byte, byte, byte) Town(int x, int y) => Scene(x, y % 150);

        Assert.Empty(DialogueReader.Choices(Town, 0, 0, Width, Height));
        Assert.False(DialogueReader.Showing(Town, 0, 0, Width, Height));
    }

    [Fact]
    public void ABoxWithOnlyTheNameIsShowingAndHasNoReplies()
    {
        static (byte, byte, byte) NameOnly(int x, int y)
        {
            if (y >= 200)
            {
                return (10, 9, 8);
            }

            return Scene(x, y);
        }

        Assert.Empty(DialogueReader.Choices(NameOnly, 0, 0, Width, Height));
        Assert.True(DialogueReader.Showing(NameOnly, 0, 0, Width, Height));
    }
}
