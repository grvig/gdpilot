using GDPilot.Output;
using Microsoft.Win32;

namespace GDPilot.Integration.Tests;

/// <summary>Against a key of its own, so running the tests never touches what starts with Windows.</summary>
public sealed class StartupEntryTests : IDisposable
{
    private const string TestKey = @"Software\GDPilotTests\Run";
    private const string Executable = @"C:\Program Files\GDPilot\GDPilot.exe";

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\GDPilotTests", false);
    }

    [Fact]
    public void ItIsOffUntilSet()
    {
        Assert.False(new StartupEntry(TestKey).IsOn(Executable));
    }

    [Fact]
    public void SettingItThenClearingItLeavesItOff()
    {
        StartupEntry entry = new(TestKey);

        entry.Set(Executable, true);
        Assert.True(entry.IsOn(Executable));

        entry.Set(Executable, false);
        Assert.False(entry.IsOn(Executable));
    }

    [Fact]
    public void AnEntryForACopyElsewhereDoesNotCountAsThisOne()
    {
        StartupEntry entry = new(TestKey);
        entry.Set(@"D:\Old\GDPilot.exe", true);

        Assert.False(entry.IsOn(Executable));
    }

    [Fact]
    public void ClearingWhatWasNeverSetIsHarmless()
    {
        new StartupEntry(TestKey).Set(Executable, false);
    }

    [Fact]
    public void APathWithSpacesIsQuoted()
    {
        Assert.Equal("\"C:\\Program Files\\GDPilot\\GDPilot.exe\"", StartupEntry.CommandFor(Executable));
    }
}
