using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class ConversationWatchTests
{
    private static readonly string[] Talk = { "Dialog" };
    private static readonly TimeSpan Step = ConversationWatch.ReadEvery;

    [Fact]
    public void ATalkWithItsBoxShowingIsAConversation()
    {
        ConversationWatch watch = new(() => true);

        Assert.Equal(Talk, watch.Filter(Talk, Step));
    }

    [Fact]
    public void ATalkWithNoBoxIsNot()
    {
        ConversationWatch watch = new(() => false);

        Assert.Empty(watch.Filter(Talk, Step));
    }

    [Fact]
    public void OneMissedReadingDoesNotEndIt()
    {
        bool box = true;
        ConversationWatch watch = new(() => box);
        watch.Filter(Talk, Step);
        box = false;

        Assert.Equal(Talk, watch.Filter(Talk, Step));
        Assert.Empty(watch.Filter(Talk, Step));
    }

    [Fact]
    public void AGoodbyeEndsItThoughTheTalkLingersAndTheNextBoxStartsIt()
    {
        bool box = true;
        ConversationWatch watch = new(() => box);
        watch.Filter(Talk, Step);
        box = false;
        watch.Filter(Talk, Step);
        watch.Filter(Talk, Step);

        box = true;

        Assert.Equal(Talk, watch.Filter(Talk, Step));
    }

    [Fact]
    public void TheScreenIsOnlyReadAFewTimesASecondAndOnlyDuringATalk()
    {
        int reads = 0;
        ConversationWatch watch = new(() =>
        {
            reads++;
            return true;
        });
        string[] nothing = Array.Empty<string>();

        for (int tick = 0; tick < 100; tick++)
        {
            watch.Filter(nothing, TimeSpan.FromMilliseconds(4));
        }

        Assert.Equal(0, reads);

        for (int tick = 0; tick < 100; tick++)
        {
            watch.Filter(Talk, TimeSpan.FromMilliseconds(4));
        }

        Assert.InRange(reads, 3, 5);
    }

    [Fact]
    public void OtherWindowsPassThroughUntouched()
    {
        ConversationWatch watch = new(() => false);

        Assert.Equal(new[] { "Inventory" }, watch.Filter(new[] { "Inventory" }, Step));
    }
}
