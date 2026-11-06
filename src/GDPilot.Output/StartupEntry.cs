using Microsoft.Win32;

namespace GDPilot.Output;

/// <summary>
/// Whether GDPilot starts with Windows: one value of the person's own Run key,
/// set only when they tick it, and taken away again when they untick it.
/// </summary>
public sealed class StartupEntry
{
    public const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "GDPilot";
    private readonly string keyPath;

    public StartupEntry(string keyPath = RunKey)
    {
        this.keyPath = keyPath;
    }

    /// <summary>The command Windows runs, quoted for a path with spaces in it.</summary>
    public static string CommandFor(string executable)
    {
        return "\"" + executable + "\"";
    }

    public bool IsOn(string executable)
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(keyPath);
        return key?.GetValue(ValueName) is string command && command == CommandFor(executable);
    }

    public void Set(string executable, bool on)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath);

        if (on)
        {
            key.SetValue(ValueName, CommandFor(executable));
        }
        else
        {
            key.DeleteValue(ValueName, false);
        }
    }
}
