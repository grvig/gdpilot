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

    [DllImport("kernel32.dll")]
    private static extern uint GetConsoleProcessList(uint[] processes, uint count);
}
