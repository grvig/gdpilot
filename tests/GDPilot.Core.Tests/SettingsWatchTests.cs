using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class SettingsWatchTests
{
    private static readonly DateTime First = new(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Start = TimeSpan.FromSeconds(10);

    private DateTime? stamp = First;
    private string text = "{}";
    private bool locked;

    private SettingsWatch Watch()
    {
        return new SettingsWatch(() => stamp, () =>
        {
            if (locked)
            {
                throw new IOException("in use");
            }

            return GDPilotConfig.Parse(text);
        });
    }

    [Fact]
    public void TheFileAsItWasAtTheStartIsNoChange()
    {
        Assert.Null(Watch().Changed(Start + TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void ASaveIsTakenOnceTheFileHasStoodStill()
    {
        SettingsWatch watch = Watch();
        stamp = First.AddSeconds(1);
        text = """{ "input": { "lookSpeed": 4 } }""";

        Assert.Null(watch.Changed(Start));
        Assert.Null(watch.Changed(Start + SettingsWatch.Settle / 2));
        Assert.Equal(4, watch.Changed(Start + SettingsWatch.Settle)!.Input.LookSpeed);
        Assert.Null(watch.Changed(Start + SettingsWatch.Settle * 2));
    }

    [Fact]
    public void AFileWrittenInTwoGoesIsReadOnlyAfterTheSecond()
    {
        SettingsWatch watch = Watch();
        stamp = First.AddSeconds(1);
        watch.Changed(Start);
        stamp = First.AddSeconds(2);

        Assert.Null(watch.Changed(Start + SettingsWatch.Settle));
        Assert.NotNull(watch.Changed(Start + SettingsWatch.Settle * 2));
    }

    [Fact]
    public void ABrokenSaveKeepsTheSettingsSaysWhyAndIsNotReadAgain()
    {
        SettingsWatch watch = Watch();
        stamp = First.AddSeconds(1);
        text = """{ "input": { "pollHz": 5 } }""";
        watch.Changed(Start);

        Assert.Null(watch.Changed(Start + SettingsWatch.Settle));
        Assert.Contains("pollHz", watch.Problem);
        Assert.Null(watch.Changed(Start + SettingsWatch.Settle * 3));
    }

    [Fact]
    public void AFileThatIsNotJsonAtAllIsABrokenSaveToo()
    {
        SettingsWatch watch = Watch();
        stamp = First.AddSeconds(1);
        text = "{ input";
        watch.Changed(Start);

        Assert.Null(watch.Changed(Start + SettingsWatch.Settle));
        Assert.NotNull(watch.Problem);
    }

    [Fact]
    public void TheNextGoodSaveClearsTheProblem()
    {
        SettingsWatch watch = Watch();
        stamp = First.AddSeconds(1);
        text = "{ input";
        watch.Changed(Start);
        watch.Changed(Start + SettingsWatch.Settle);

        stamp = First.AddSeconds(5);
        text = "{}";
        watch.Changed(Start + TimeSpan.FromSeconds(1));

        Assert.NotNull(watch.Changed(Start + TimeSpan.FromSeconds(2)));
        Assert.Null(watch.Problem);
    }

    [Fact]
    public void AFileStillHeldOpenIsTriedAgain()
    {
        SettingsWatch watch = Watch();
        stamp = First.AddSeconds(1);
        locked = true;
        watch.Changed(Start);

        Assert.Null(watch.Changed(Start + SettingsWatch.Settle));

        locked = false;
        watch.Changed(Start + SettingsWatch.Settle * 2);
        Assert.NotNull(watch.Changed(Start + SettingsWatch.Settle * 3));
    }

    [Fact]
    public void AFileTakenAwayChangesNothing()
    {
        SettingsWatch watch = Watch();
        stamp = null;

        Assert.Null(watch.Changed(Start));
        Assert.Null(watch.Changed(Start + TimeSpan.FromSeconds(5)));
    }
}
