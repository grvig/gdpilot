using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace GDPilot.Output;

/// <summary>
/// Loads a DLL into another running process by having that process call
/// LoadLibraryW on it: the standard way, and the one GrimAction uses. The DLL's
/// own start-up does everything else. Nothing is ever unloaded, because pulling
/// code out of a running game is how games crash.
/// </summary>
public static class GameLoader
{
    private const uint CreateThreadAndWrite = 0x0002 | 0x0008 | 0x0010 | 0x0020 | 0x0400;
    private const uint Reserve = 0x1000 | 0x2000;
    private const uint ReadWrite = 0x04;
    private const uint Release = 0x8000;

    /// <summary>Whether a module whose file name starts with this is already loaded in the process.</summary>
    public static bool IsLoaded(Process process, string fileNameStart)
    {
        process.Refresh();

        foreach (ProcessModule module in process.Modules)
        {
            if (module.ModuleName.StartsWith(fileNameStart, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Loads the DLL and waits for its loading to finish, returning a reason on failure.</summary>
    public static string? Load(Process process, string dllPath)
    {
        string fullPath = Path.GetFullPath(dllPath);

        if (!File.Exists(fullPath))
        {
            return $"{fullPath} does not exist.";
        }

        IntPtr handle = OpenProcess(CreateThreadAndWrite, false, process.Id);

        if (handle == IntPtr.Zero)
        {
            return $"Could not open the process: error {Marshal.GetLastWin32Error()}.";
        }

        IntPtr memory = IntPtr.Zero;

        try
        {
            byte[] path = Encoding.Unicode.GetBytes(fullPath + "\0");
            memory = VirtualAllocEx(handle, IntPtr.Zero, (UIntPtr)path.Length, Reserve, ReadWrite);

            if (memory == IntPtr.Zero || !WriteProcessMemory(handle, memory, path, (UIntPtr)path.Length, out _))
            {
                return $"Could not write the DLL's path into the process: error {Marshal.GetLastWin32Error()}.";
            }

            // kernel32 sits at the same address in every process of a session, so its LoadLibraryW does too.
            IntPtr loadLibrary = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW");
            IntPtr thread = CreateRemoteThread(handle, IntPtr.Zero, UIntPtr.Zero, loadLibrary, memory, 0, IntPtr.Zero);

            if (thread == IntPtr.Zero)
            {
                return $"Could not start the loading thread: error {Marshal.GetLastWin32Error()}.";
            }

            WaitForSingleObject(thread, 10000);
            GetExitCodeThread(thread, out uint result);
            CloseHandle(thread);

            if (result == 0)
            {
                return "The process could not load the DLL.";
            }

            return null;
        }
        finally
        {
            if (memory != IntPtr.Zero)
            {
                VirtualFreeEx(handle, memory, UIntPtr.Zero, Release);
            }

            CloseHandle(handle);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint access, bool inherit, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAllocEx(IntPtr process, IntPtr address, UIntPtr size, uint type, uint protect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualFreeEx(IntPtr process, IntPtr address, UIntPtr size, uint type);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(IntPtr process, IntPtr address, byte[] buffer, UIntPtr size, out UIntPtr written);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateRemoteThread(IntPtr process, IntPtr attributes, UIntPtr stackSize, IntPtr start, IntPtr parameter, uint flags, IntPtr threadId);

    [DllImport("kernel32.dll")]
    private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [DllImport("kernel32.dll")]
    private static extern bool GetExitCodeThread(IntPtr thread, out uint exitCode);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string name);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
    private static extern IntPtr GetProcAddress(IntPtr module, string name);
}
