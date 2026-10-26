using GDPilot.Core;
using GDPilot.Output;

namespace GDPilot.App;

/// <summary>
/// Sends commands for the in-game part down the pipe and everything else on to
/// Windows. It sits behind the guarded sink, so the kill switch stops commands
/// the same way it stops key presses. Without a link, as in a dry run, commands
/// go to the inner sink, which prints them.
/// </summary>
internal sealed class CommandRouter : IInputSink
{
    private readonly IInputSink inner;

    public CommandRouter(IInputSink inner)
    {
        this.inner = inner;
    }

    public GameLink? Link { get; set; }

    public void Send(InputIntent intent)
    {
        if (intent is GameCommandIntent command && Link is not null)
        {
            Link.Send(command.Command);
            return;
        }

        inner.Send(intent);
    }
}
