using GDPilot.Core;

namespace GDPilot.App;

/// <summary>
/// Finds the configuration: the person's own config.json first, then the
/// tracked example shipped beside the executable, then built-in defaults.
/// </summary>
internal static class ConfigFile
{
    internal static GDPilotConfig Load(TextWriter report)
    {
        string[] candidates =
        {
            Path.Combine(Environment.CurrentDirectory, "config.json"),
            Path.Combine(AppContext.BaseDirectory, "config.json"),
            Path.Combine(AppContext.BaseDirectory, "config.example.json"),
        };

        foreach (string path in candidates)
        {
            if (File.Exists(path))
            {
                report.WriteLine($"Configuration: {path}");
                return GDPilotConfig.Parse(File.ReadAllText(path));
            }
        }

        report.WriteLine("Configuration: none found, using built-in defaults with no button bindings.");
        return GDPilotConfig.Parse("{}");
    }
}
