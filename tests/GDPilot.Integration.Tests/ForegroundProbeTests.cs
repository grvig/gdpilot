using GDPilot.Output;

namespace GDPilot.Integration.Tests;

public sealed class ForegroundProbeTests
{
    [Fact]
    public void AProcessThatIsNotRunningIsNeverInFront()
    {
        Assert.False(ForegroundProbe.IsInFront("gdpilot-no-such-process"));
    }

    [Fact]
    public void AnEmptyNameIsNeverInFront()
    {
        Assert.False(ForegroundProbe.IsInFront(string.Empty));
    }
}
