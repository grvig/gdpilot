using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class PadGlyphsTests
{
    [Theory]
    [InlineData("SDL_GAMEPAD_TYPE_XBOXONE", "Xbox Wireless Controller", PadStyle.Xbox)]
    [InlineData("SDL_GAMEPAD_TYPE_PS5", "DualSense Wireless Controller", PadStyle.PlayStation)]
    [InlineData("SDL_GAMEPAD_TYPE_PS4", null, PadStyle.PlayStation)]
    [InlineData("SDL_GAMEPAD_TYPE_NINTENDO_SWITCH_PRO", "Pro Controller", PadStyle.Nintendo)]
    [InlineData("SDL_GAMEPAD_TYPE_STANDARD", "Some Pad", PadStyle.Xbox)]
    [InlineData("SDL_GAMEPAD_TYPE_STANDARD", "PS5 clone", PadStyle.PlayStation)]
    public void TheFamilyComesFromTheTypeThenTheName(string type, string? name, PadStyle style)
    {
        Assert.Equal(style, PadGlyphs.StyleOf(type, name));
    }

    [Theory]
    [InlineData(GamepadButtons.A, PadStyle.Xbox, "A")]
    [InlineData(GamepadButtons.A, PadStyle.PlayStation, "✕")]
    [InlineData(GamepadButtons.A, PadStyle.Nintendo, "B")]
    [InlineData(GamepadButtons.Y, PadStyle.Nintendo, "X")]
    [InlineData(GamepadButtons.LeftBumper, PadStyle.PlayStation, "L1")]
    [InlineData(GamepadButtons.Back, PadStyle.Xbox, "View")]
    [InlineData(GamepadButtons.Start, PadStyle.PlayStation, "Options")]
    [InlineData(GamepadButtons.DPadUp, PadStyle.PlayStation, "D-pad ↑")]
    public void ButtonsReadAsTheyArePrinted(GamepadButtons button, PadStyle style, string name)
    {
        Assert.Equal(name, PadGlyphs.Name(button, style));
    }

    [Fact]
    public void TheFaceButtonsHaveTheirPrintedColours()
    {
        Assert.NotEqual(PadGlyphs.Colour("A"), PadGlyphs.Colour("✕"));
        Assert.Equal(PadGlyphs.Colour("B"), PadGlyphs.Colour("○"));
        Assert.Null(PadGlyphs.Colour("LB"));
    }

    [Fact]
    public void TheLegendAndThePromptUseThePadsNames()
    {
        ButtonLayers layers = BindingTable.Parse(new Dictionary<string, string> { ["A"] = "interact:Space", ["LT+X"] = "key:5" }, 0.5);

        OverlayState state = OverlayState.Build(Mode.Combat, SuspendReason.None, layers, style: PadStyle.PlayStation);

        Assert.Contains(state.Legend, line => line.StartsWith("✕: ", StringComparison.Ordinal));
        Assert.Contains("L2+□: 5", state.Legend);
        Assert.StartsWith("✕", OverlayState.PromptFor(new NearbyThing(1, ThingKind.Talk, 0.5, 0.5, 1), layers, PadStyle.PlayStation));
    }

    [Theory]
    [InlineData("playstation", PadStyle.PlayStation)]
    [InlineData("Nintendo", PadStyle.Nintendo)]
    [InlineData("xbox", PadStyle.Xbox)]
    [InlineData("auto", null)]
    [InlineData(null, null)]
    public void ASettingCanNameTheFamily(string? setting, PadStyle? style)
    {
        Assert.Equal(style, PadGlyphs.Chosen(setting));
    }
}
