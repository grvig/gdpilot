using System.Runtime.InteropServices;

namespace GDPilot.Output;

/// <summary>How many programs share this console, which says whether a person's command prompt is one of them.</summary>
public static class ConsoleOwners
{
    public static int Count()
    {
        uint[] owners = new uint[8];
        return (int)GetConsoleProcessList(owners, (uint)owners.Length);
    }

    /// <summary>Gives up the console, whose window goes with its last program.</summary>
    public static void Leave()
    {
        FreeConsole();
    }

    [DllImport("kernel32.dll")]
    private static extern uint GetConsoleProcessList(uint[] processes, uint count);

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();
}
