using System.Text;

namespace GDPilot.Core;

public enum LoaderStep
{
    /// <summary>Nothing there yet: put the loader in.</summary>
    Install,

    /// <summary>An earlier copy of the loader is there: put this one in its place.</summary>
    Replace,

    /// <summary>Nothing of GDPilot's is there: nothing to take away.</summary>
    Nothing,

    /// <summary>The loader is there: take it away.</summary>
    Remove,

    /// <summary>Someone else's dinput8.dll is there, another mod's most likely: leave it alone.</summary>
    Refuse,
}

/// <summary>
/// Whether the drop-in loader may go into, or come out of, the game's folder. It
/// is only ever put where nothing is, or over a copy of itself, and only ever
/// taken away when it is its own, which it tells by the mark it carries.
/// </summary>
public static class LoaderPlace
{
    public const string FileName = "dinput8.dll";

    private static readonly byte[] Mark = Encoding.ASCII.GetBytes("GDPilot drop-in loader");

    public static bool IsOurs(byte[] file)
    {
        return file.AsSpan().IndexOf(Mark) >= 0;
    }

    /// <param name="present">What is at the loader's place in the game's folder now, or null when nothing is.</param>
    public static LoaderStep ToInstall(byte[]? present)
    {
        if (present is null)
        {
            return LoaderStep.Install;
        }

        if (IsOurs(present))
        {
            return LoaderStep.Replace;
        }

        return LoaderStep.Refuse;
    }

    public static LoaderStep ToRemove(byte[]? present)
    {
        if (present is null)
        {
            return LoaderStep.Nothing;
        }

        if (IsOurs(present))
        {
            return LoaderStep.Remove;
        }

        return LoaderStep.Refuse;
    }
}
