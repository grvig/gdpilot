using GDPilot.Core;

namespace GDPilot.Core.Tests;

public sealed class CommandLineTests
{
    [Fact]
    public void KnownOptionsAndTheirValuesPass()
    {
        Assert.Null(CommandLine.Unknown(new[] { "--replay", "trace.json", "--seconds", "4", "--dry-run" }));
        Assert.Null(CommandLine.Unknown(new[] { "--install-loader" }));
        Assert.Null(CommandLine.Unknown(new[] { "--remove-loader" }));
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("--setup-pad")]
    [InlineData("--dryrun")]
    public void AnUnknownOptionIsNamed(string option)
    {
        Assert.Equal(option, CommandLine.Unknown(new[] { "--dry-run", option }));
    }

    [Fact]
    public void ANumberIsReadWhenThereAndAbsentWhenNot()
    {
        Assert.Equal((2.5, null), CommandLine.Number(new[] { "--seconds", "2.5" }, "--seconds"));
        Assert.Equal((null, null), CommandLine.Number(new[] { "--dry-run" }, "--seconds"));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-3")]
    [InlineData("0")]
    public void ANumberThatIsNotOneSaysSo(string given)
    {
        Assert.NotNull(CommandLine.Number(new[] { "--seconds", given }, "--seconds").Problem);
    }

    [Fact]
    public void AMissingNumberSaysSo()
    {
        Assert.NotNull(CommandLine.Number(new[] { "--seconds" }, "--seconds").Problem);
    }

    [Fact]
    public void WholeNumbersAreReadInTheirCount()
    {
        Assert.Equal(new[] { 10, 20, 30, 40 }, CommandLine.Whole(new[] { "--at", "10", "20", "30", "40" }, "--at", 4).Values);
        Assert.NotNull(CommandLine.Whole(new[] { "--at", "10", "x", "30", "40" }, "--at", 4).Problem);
        Assert.NotNull(CommandLine.Whole(new[] { "--counts", "10" }, "--counts", 2).Problem);
    }
}
