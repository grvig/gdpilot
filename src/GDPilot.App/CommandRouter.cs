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

    private bool explained;

    public void Send(InputIntent intent)
    {
        if (intent is GameCommandIntent command && Link is not null)
        {
            // A camera press with nothing to receive it would otherwise do nothing, silently.
            if (!Link.Send(command.Command) && command.Command is ToggleCameraCommand && !explained)
            {
                Console.WriteLine("The camera button needs the in-game part: start the game, then run GDPilot.exe --load-in-game.");
                explained = true;
            }

            return;
        }

        inner.Send(intent);
    }
}
