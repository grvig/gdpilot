using System.IO.Pipes;
using GDPilot.Core;

namespace GDPilot.Output;

/// <summary>
/// The local connection to the in-game part: a named pipe only the current
/// Windows user can open. Reports arrive on its own thread; commands go out
/// from whichever thread sends them. It accepts one connection at a time and
/// listens again whenever one ends, so the game can be restarted without
/// restarting GDPilot.
/// </summary>
public sealed class GameLink : IDisposable
{
    public const string PipeName = "GDPilot.Game";

    private const int Buffer = 64 * 1024;

    private readonly string pipeName;
    private readonly ReportedOccupancy layouts;
    private readonly Action<string> say;
    private readonly CancellationTokenSource stop = new();
    private readonly Thread listener;
    private readonly object sending = new();
    private PanelsReport? panels;
    private TargetsReport near = new(Array.Empty<SeenTarget>());
    private IReadOnlyList<NearbyThing> things = Array.Empty<NearbyThing>();
    private LockPointReport? lockPoint;
    private StreamWriter? writer;
    private volatile bool thirdPerson;
    private volatile int health = -1;
    private volatile bool analogMovement;

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

    /// <summary>The player's life in percent, as last reported, or -1 when unknown.</summary>
    public int Health => health;

    /// <summary>The enemies near the player as last reported, or none once the game has gone.</summary>
    public IReadOnlyList<SeenTarget> Targets => Volatile.Read(ref near).Targets;

    /// <summary>The last targets report whole, with where the character and the ground round it fall on the screen.</summary>
    public TargetsReport Near => Volatile.Read(ref near);

    /// <summary>Where the locked enemy was last drawn, every frame it moves; null once the game has gone.</summary>
    public LockPointReport? LockPoint => Volatile.Read(ref lockPoint);

    /// <summary>What lies close by to pick up, talk to or use, as last reported; nothing once the game has gone.</summary>
    public IReadOnlyList<NearbyThing> Things => Volatile.Read(ref things);

    /// <summary>Whether the in-game part can steer the character from the stick; false once it has gone.</summary>
    public bool AnalogMovement => analogMovement;

    /// <summary>Whether the game last said it was showing the third-person camera; false once it has gone.</summary>
    public bool ThirdPerson => thirdPerson;

    /// <summary>Sends a command, or returns false when no in-game part is connected to receive it.</summary>
    public bool Send(GameCommand command)
    {
        lock (sending)
        {
            if (writer is null)
            {
                return false;
            }

            try
            {
                writer.WriteLine(command.Write());
                writer.Flush();
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }
    }

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
                // A pipe without a buffer makes every write wait for the other side to
                // read it, which would stall the control loop whenever the game is busy.
                using NamedPipeServerStream pipe = new(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly, Buffer, Buffer);
                pipe.WaitForConnectionAsync(stop.Token).GetAwaiter().GetResult();

                lock (sending)
                {
                    writer = new StreamWriter(pipe, leaveOpen: true);
                }

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

                lock (sending)
                {
                    writer = null;
                }

                thirdPerson = false;
                analogMovement = false;
                health = -1;
                Volatile.Write(ref panels, null);
                Volatile.Write(ref near, new TargetsReport(Array.Empty<SeenTarget>()));
                Volatile.Write(ref things, Array.Empty<NearbyThing>());
                Volatile.Write(ref lockPoint, null);
                layouts.Forget();
            }
        }
    }

    private void Read(NamedPipeServerStream pipe)
    {
        using StreamReader reader = new(pipe, leaveOpen: true);
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
            case CameraReport camera:
                thirdPerson = camera.ThirdPerson;
                break;
            case HealthReport life:
                health = life.Percent;
                break;
            case LockPointReport point:
                Volatile.Write(ref lockPoint, point);
                break;
            case InteractablesReport close:
                Volatile.Write(ref things, close.Things);
                break;
            case AbilitiesReport abilities:
                analogMovement = abilities.AnalogMovement;
                break;
            case TargetsReport around:
                Volatile.Write(ref near, around);
                break;
        }
    }
}
