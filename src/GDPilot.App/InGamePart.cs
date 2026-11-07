namespace GDPilot.App;

/// <summary>
/// The in-game part as the game loads it: a copy named after its contents, in
/// GDPilot's own folder, since the game keeps the file it loaded open until it
/// exits and loading the build output directly would stop the next build from
/// replacing it. The drop-in loader is told which copy is current.
/// </summary>
internal static class InGamePart
{
    internal const string LoadedPrefix = "GDPilot.InGame.";
    private const string DllName = "GDPilot.InGame.dll";

    private static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GDPilot", "in-game");

    /// <summary>The copy for this build, made if it is not there yet; null when this build has no in-game part beside it.</summary>
    internal static string? Stage()
    {
        string built = Path.Combine(AppContext.BaseDirectory, DllName);

        if (!File.Exists(built))
        {
            return null;
        }

        byte[] bytes = File.ReadAllBytes(built);
        string version = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes))[..12].ToLowerInvariant();
        string staged = Path.Combine(Folder, $"{LoadedPrefix}{version}.dll");
        Directory.CreateDirectory(Folder);

        if (!File.Exists(staged))
        {
            File.WriteAllBytes(staged, bytes);
        }

        return staged;
    }

    /// <summary>Leaves this build's copy where the drop-in loader looks, so an updated GDPilot is what the game loads next.</summary>
    internal static void PointLoaderAtCurrent()
    {
        try
        {
            if (Stage() is string staged)
            {
                File.WriteAllText(Path.Combine(Folder, "current.txt"), staged);
            }
        }
        catch (IOException problem)
        {
            Console.Error.WriteLine($"Could not tell the drop-in loader where the in-game part is: {problem.Message}");
        }
        catch (UnauthorizedAccessException problem)
        {
            Console.Error.WriteLine($"Could not tell the drop-in loader where the in-game part is: {problem.Message}");
        }
    }
}
