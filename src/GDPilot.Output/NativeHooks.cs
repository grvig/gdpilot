using System.Runtime.InteropServices;

namespace GDPilot.Output;

internal static class NativeHooks
{
    internal const int KeyboardLowLevel = 13;
    internal const uint KeyUpFlag = 0x80;
    internal const uint QuitMessage = 0x0012;

    internal delegate IntPtr HookProcedure(int code, IntPtr message, IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    internal struct KeyboardData
    {
        internal uint VirtualKey;
        internal uint ScanCode;
        internal uint Flags;
        internal uint Time;
        internal UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Message
    {
        internal IntPtr Window;
        internal uint Id;
        internal IntPtr WordParameter;
        internal IntPtr LongParameter;
        internal uint Time;
        internal int X;
        internal int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetWindowsHookEx(int hookType, HookProcedure procedure, IntPtr module, uint threadId);

    [DllImport("user32.dll")]
    internal static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    internal static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);

    [DllImport("user32.dll")]
    internal static extern int GetMessage(out Message message, IntPtr window, uint first, uint last);

    [DllImport("user32.dll")]
    internal static extern bool PostThreadMessage(uint threadId, uint message, IntPtr wordParameter, IntPtr longParameter);

    [DllImport("kernel32.dll")]
    internal static extern uint GetCurrentThreadId();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    internal static extern IntPtr GetModuleHandle(string? moduleName);
}
