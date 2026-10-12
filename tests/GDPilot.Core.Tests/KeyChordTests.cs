using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class KeyChordTests
{
    [Theory]
    [InlineData("Ctrl+Alt+Pause")]
    [InlineData("ctrl + alt + pause")]
    [InlineData("Control+Alt+Pause")]
    [InlineData("Alt+Ctrl+Pause")]
    public void TheDefaultKillSwitchParsesInAnyCommonSpelling(string text)
    {
        KeyChord chord = KeyChord.Parse(text);

        Assert.Equal(new KeyChord(KeyModifiers.Control | KeyModifiers.Alt, KeyCode.Pause), chord);
    }

    [Fact]
    public void AKeyOnItsOwnHasNoModifiers()
    {
        Assert.Equal(new KeyChord(KeyModifiers.None, KeyCode.F12), KeyChord.Parse("F12"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+")]
    [InlineData("Ctrl+Ctrl+Pause")]
    [InlineData("Hyper+Pause")]
    [InlineData("Ctrl+NotAKey")]
    [InlineData("Ctrl+19")]
    [InlineData("Ctrl+None")]
    public void MalformedChordsAreRejected(string text)
    {
        Assert.Throws<FormatException>(() => KeyChord.Parse(text));
    }

    [Fact]
    public void MatchesWithExactlyItsModifiersHeld()
    {
        KeyChord chord = KeyChord.Parse("Ctrl+Alt+Pause");

        Assert.True(chord.Matches(KeyCode.Pause, KeyModifiers.Control | KeyModifiers.Alt));
    }

    [Fact]
    public void ExtraModifiersStillMatch()
    {
        KeyChord chord = KeyChord.Parse("Ctrl+Alt+Pause");

        Assert.True(chord.Matches(KeyCode.Pause, KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Shift));
    }

    [Fact]
    public void AMissingModifierOrAnotherKeyDoesNotMatch()
    {
        KeyChord chord = KeyChord.Parse("Ctrl+Alt+Pause");

        Assert.False(chord.Matches(KeyCode.Pause, KeyModifiers.Control));
        Assert.False(chord.Matches(KeyCode.Escape, KeyModifiers.Control | KeyModifiers.Alt));
    }
}
