using GDPilot.Core;

namespace GDPilot.App;

/// <summary>
/// Finds the configuration: the person's own config.json first, in this folder,
/// beside the executable or in GDPilot's own folder. With none, the first run
/// puts a copy of the tracked example in GDPilot's own folder for them to edit.
/// </summary>
internal static class ConfigFile
{
    internal static string OwnFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GDPilot");

    /// <summary>The file the configuration was read from, or null if none was.</summary>
    internal static string? LoadedFrom { get; private set; }

    /// <summary>Whether this is the run that first gave the person their settings.</summary>
    internal static bool FirstRun { get; private set; }

    internal static GDPilotConfig Load(TextWriter report)
    {
        string own = Path.Combine(OwnFolder, "config.json");
        string example = Path.Combine(AppContext.BaseDirectory, "config.example.json");
        string[] candidates =
        {
            Path.Combine(Environment.CurrentDirectory, "config.json"),
            Path.Combine(AppContext.BaseDirectory, "config.json"),
            own,
        };

        if (!candidates.Any(File.Exists) && File.Exists(example))
        {
            Directory.CreateDirectory(OwnFolder);
            File.WriteAllText(own, ConfigLayers.ForAPerson(File.ReadAllText(example)));
            FirstRun = true;
            report.WriteLine($"First run: your settings are now in {own}.");
        }

        foreach (string path in candidates)
        {
            if (File.Exists(path))
            {
                report.WriteLine($"Configuration: {path}");
                LoadedFrom = path;
                return GDPilotConfig.Parse(Layered(File.ReadAllText(path), example));
            }
        }

        report.WriteLine("Configuration: none found, using built-in defaults with no button bindings.");
        return GDPilotConfig.Parse("{}");
    }

    private static string Layered(string own, string example)
    {
        if (!File.Exists(example))
        {
            return own;
        }

        try
        {
            return ConfigLayers.Over(File.ReadAllText(example), own);
        }
        catch (System.Text.Json.JsonException problem)
        {
            throw new FormatException(problem.Message);
        }
    }
}
