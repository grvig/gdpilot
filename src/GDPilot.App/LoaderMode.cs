using GDPilot.Core;

namespace GDPilot.App;

/// <summary>
/// --install-loader and --remove-loader: puts GDPilot's drop-in loader into the
/// game's x64 folder as dinput8.dll, or takes it away. With it, the in-game part
/// loads with the game, with nothing reaching into a running process.
/// </summary>
internal static class LoaderMode
{
    private const string Built = "GDPilot.Loader.dll";

    internal static int Install(GDPilotConfig config)
    {
        if (Place(config) is not string place)
        {
            return 2;
        }

        string built = Path.Combine(AppContext.BaseDirectory, Built);

        if (!File.Exists(built))
        {
            Console.Error.WriteLine($"{Built} is missing from beside GDPilot.exe. Build it with src\\GDPilot.Loader\\build.cmd.");
            return 2;
        }

        LoaderStep step = LoaderPlace.ToInstall(Present(place));

        if (step == LoaderStep.Refuse)
        {
            Console.Error.WriteLine($"{place} is there already and is not GDPilot's, most likely another mod's. It has been left alone, and the loader not installed.");
            return 1;
        }

        try
        {
            File.Copy(built, place, true);
        }
        catch (IOException)
        {
            Console.Error.WriteLine("The game is holding the loader open. Close the game, then install it again.");
            return 1;
        }

        InGamePart.PointLoaderAtCurrent();

        if (step == LoaderStep.Replace)
        {
            Console.WriteLine($"Replaced the earlier loader at {place}. The game picks it up the next time it starts.");
        }
        else
        {
            Console.WriteLine($"Installed the loader at {place}. From the next start of the game, the in-game part loads with it.");
        }

        return 0;
    }

    internal static int Remove(GDPilotConfig config)
    {
        if (Place(config) is not string place)
        {
            return 2;
        }

        LoaderStep step = LoaderPlace.ToRemove(Present(place));

        if (step == LoaderStep.Refuse)
        {
            Console.Error.WriteLine($"{place} is not GDPilot's, so it has been left alone.");
            return 1;
        }

        if (step == LoaderStep.Nothing)
        {
            Console.WriteLine("The loader is not installed; nothing to take away.");
            return 0;
        }

        try
        {
            File.Delete(place);
        }
        catch (IOException)
        {
            Console.Error.WriteLine("The game is holding the loader open. Close the game, then remove it again.");
            return 1;
        }

        Console.WriteLine($"Removed the loader from {place}. The game's folder is as it was.");
        return 0;
    }

    private static string? Place(GDPilotConfig config)
    {
        string? folder = CheckGameMode.FindGame(config);

        if (folder is null || !File.Exists(Path.Combine(folder, "Game.dll")))
        {
            Console.Error.WriteLine("Could not find Grim Dawn's x64 folder.");
            return null;
        }

        return Path.Combine(folder, LoaderPlace.FileName);
    }

    private static byte[]? Present(string place)
    {
        if (!File.Exists(place))
        {
            return null;
        }

        return File.ReadAllBytes(place);
    }
}
