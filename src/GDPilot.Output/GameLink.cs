using System.IO.Pipes;
using GDPilot.Core;

namespace GDPilot.Output;

/// <summary>
/// The local connection the in-game part reports through: a named pipe only
/// the current Windows user can open. It runs on its own thread, accepts one
/// connection at a time, and starts listening again whenever one ends, so the
/// game can be restarted without restarting GDPilot.
/// </summary>
public sealed class GameLink : IDisposable
{
    public const string PipeName = "GDPilot.Game";

    private readonly string pipeName;
    private readonly ReportedOccupancy layouts;
    private readonly Action<string> say;
    private readonly CancellationTokenSource stop = new();
    private readonly Thread listener;
    private PanelsReport? panels;

    public GameLink(ReportedOccupancy layouts, Action<string> say, string pipeName = PipeName)
    {
        this.layouts = layouts;
        this.say = say;
        this.pipeName = pipeName;
        listener = new Thread(Listen) { IsBackground = true, Name = "GameLink" };
        listener.Start();
    }

    public bool Connected { get; private set; }

    /// <summary>The panels the game last said were open, or null when it has not said or has gone.</summary>
    public PanelsReport? Panels => Volatile.Read(ref panels);

    public void Dispose()
    {
        stop.Cancel();
        listener.Join(TimeSpan.FromSeconds(2));
        stop.Dispose();
    }

    private void Listen()
    {
        while (!stop.IsCancellationRequested)
        {
            try
            {
                using NamedPipeServerStream pipe = new(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                pipe.WaitForConnectionAsync(stop.Token).GetAwaiter().GetResult();
                Connected = true;
                say("In-game part connected: items and panels now come from the game.");
                Read(pipe);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (IOException problem)
            {
                say($"In-game connection dropped: {problem.Message}");
            }
            finally
            {
                // Whatever the game said before it went may no longer be true.
                if (Connected)
                {
                    say("In-game part disconnected: back to reading the screen.");
                }

                Connected = false;
                Volatile.Write(ref panels, null);
                layouts.Forget();
            }
        }
    }

    private void Read(NamedPipeServerStream pipe)
    {
        using StreamReader reader = new(pipe);
        bool warned = false;

        while (!stop.IsCancellationRequested)
        {
            string? line = reader.ReadLineAsync(stop.Token).AsTask().GetAwaiter().GetResult();

            if (line is null)
            {
                return;
            }

            try
            {
                Dispatch(GameReport.Parse(line));
            }
            catch (FormatException problem)
            {
                // Said once per connection, so a mismatched pair does not flood the console.
                if (!warned)
                {
                    say($"Ignoring a report from the game: {problem.Message}");
                    warned = true;
                }
            }
        }
    }

    private void Dispatch(GameReport? report)
    {
        switch (report)
        {
            case LayoutReport layout:
                layouts.Accept(layout);
                break;
            case PanelsReport open:
                Volatile.Write(ref panels, open);
                break;
        }
    }
}
