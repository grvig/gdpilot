using System.Collections.Concurrent;
using System.IO.Pipes;
using GDPilot.Core;
using GDPilot.Output;

namespace GDPilot.Integration.Tests;

/// <summary>
/// The real named pipe, with this test standing in for the in-game part. Each
/// test uses its own pipe name so runs never meet a GDPilot that is running.
/// </summary>
public sealed class GameLinkTests
{
    private static readonly GridSpec Bag = new(1044, 645, 32, 32, 8, 8);

    [Fact]
    public void AReportedLayoutReachesNavigationAndIsDroppedOnDisconnect()
    {
        string name = "GDPilot.Test." + Guid.NewGuid().ToString("N");
        ReportedOccupancy layouts = new(null);
        ConcurrentQueue<string> said = new();

        using (GameLink link = new(layouts, said.Enqueue, name))
        {
            using (NamedPipeClientStream game = new(".", name, PipeDirection.InOut))
            {
                game.Connect(2000);
                using StreamWriter writer = new(game) { AutoFlush = true };
                writer.WriteLine(new LayoutReport("Inventory.Bag2", 8, 8, new[] { new ItemRect(11, 0, 0, 1, 3) }).Write());
                writer.WriteLine(new PanelsReport(new[] { "Inventory" }).Write());

                Assert.True(SpinWait.SpinUntil(() => link.Panels is not null, 2000));
                Assert.Equal(CellState.Occupied, layouts.Read("Inventory.Bag2", Bag, null)![new GridCell(0, 2)]);
                Assert.True(link.Connected);
            }

            Assert.True(SpinWait.SpinUntil(() => !link.Connected, 2000));
            Assert.Null(layouts.Read("Inventory.Bag2", Bag, null));
            Assert.Null(link.Panels);
        }

        Assert.Contains(said, line => line.Contains("connected"));
    }

    [Fact]
    public void AMismatchedReportIsMentionedOnceAndTheConnectionCarriesOn()
    {
        string name = "GDPilot.Test." + Guid.NewGuid().ToString("N");
        ReportedOccupancy layouts = new(null);
        ConcurrentQueue<string> said = new();

        using GameLink link = new(layouts, said.Enqueue, name);
        using NamedPipeClientStream game = new(".", name, PipeDirection.InOut);
        game.Connect(2000);
        using StreamWriter writer = new(game) { AutoFlush = true };
        writer.WriteLine("""{"protocol":9,"type":"panels","open":[]}""");
        writer.WriteLine("""{"protocol":9,"type":"panels","open":[]}""");
        writer.WriteLine(new PanelsReport(new[] { "Stash" }).Write());

        Assert.True(SpinWait.SpinUntil(() => link.Panels is not null, 2000));

        Assert.Single(said, line => line.Contains("protocol 9"));
    }

    [Fact]
    public void CommandsReachTheGameAndItsCameraReportComesBack()
    {
        string name = "GDPilot.Test." + Guid.NewGuid().ToString("N");
        ConcurrentQueue<string> said = new();

        using GameLink link = new(new ReportedOccupancy(null), said.Enqueue, name);
        Assert.False(link.Send(new ToggleCameraCommand()));

        using NamedPipeClientStream game = new(".", name, PipeDirection.InOut);
        game.Connect(2000);
        Assert.True(SpinWait.SpinUntil(() => link.Connected, 2000));
        using StreamReader reader = new(game, leaveOpen: true);
        using StreamWriter writer = new(game, leaveOpen: true) { AutoFlush = true };

        Assert.True(link.Send(new LookCommand(1.5, 0)));
        Assert.Equal(new LookCommand(1.5, 0), GameCommands.Parse(reader.ReadLine()!));

        writer.WriteLine(new CameraReport(true).Write());
        Assert.True(SpinWait.SpinUntil(() => link.ThirdPerson, 2000));
    }
}
