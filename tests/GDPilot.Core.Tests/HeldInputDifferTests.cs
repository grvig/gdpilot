using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class HeldInputDifferTests
{
    private static readonly HashSet<PointerButton> NoButtons = new();

    private readonly HeldInputDiffer differ = new();

    [Fact]
    public void NewlyWantedKeysArePressed()
    {
        List<InputIntent> intents = differ.Update(Keys(KeyCode.W), NoButtons);

        Assert.Equal(new InputIntent[] { new KeyDown(KeyCode.W) }, intents);
    }

    [Fact]
    public void UnchangedStateProducesNothing()
    {
        differ.Update(Keys(KeyCode.W), NoButtons);

        Assert.Empty(differ.Update(Keys(KeyCode.W), NoButtons));
    }

    [Fact]
    public void ReleasesComeBeforePresses()
    {
        differ.Update(Keys(KeyCode.W, KeyCode.A), NoButtons);

        List<InputIntent> intents = differ.Update(Keys(KeyCode.W, KeyCode.D), NoButtons);

        Assert.Equal(new InputIntent[] { new KeyUp(KeyCode.A), new KeyDown(KeyCode.D) }, intents);
    }

    [Fact]
    public void PointerButtonsArePressedAndReleasedToo()
    {
        HashSet<PointerButton> left = new() { PointerButton.Left };
        Assert.Equal(new InputIntent[] { new PointerDown(PointerButton.Left) }, differ.Update(Keys(), left));

        Assert.Equal(new InputIntent[] { new PointerUp(PointerButton.Left) }, differ.Update(Keys(), NoButtons));
    }

    [Fact]
    public void LettingEverythingGoReleasesEverything()
    {
        differ.Update(Keys(KeyCode.S, KeyCode.D), NoButtons);

        List<InputIntent> intents = differ.Update(Keys(), NoButtons);

        Assert.Equal(new InputIntent[] { new KeyUp(KeyCode.D), new KeyUp(KeyCode.S) }, intents);
    }

    private static HashSet<KeyCode> Keys(params KeyCode[] keys)
    {
        return new HashSet<KeyCode>(keys);
    }
}
