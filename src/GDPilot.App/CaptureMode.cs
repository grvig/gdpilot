using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GDPilot.Core;
using GDPilot.Output;
using GDPilot.Vision;

namespace GDPilot.App;

/// <summary>
/// Section 11's --capture: saves the game's client area as it is right now,
/// with the resolution and UI scale beside it, as a calibration fixture.
/// Nothing in it needs interpreting by whoever runs it.
/// </summary>
internal static class CaptureMode
{
    internal static int Run(GDPilotConfig config, string name, string directory)
    {
        ScreenRect? game = GameWindow.ClientBounds(config.GameProcess);
        LiveDisplay? display = GameDisplay.Read(config);

        if (game is not ScreenRect bounds || display is null)
        {
            Console.Error.WriteLine($"Could not find a running {config.GameProcess} window and its settings file.");
            return 1;
        }

        CapturedFrame frame = ScreenCapture.Region(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        Directory.CreateDirectory(directory);
        string image = Path.Combine(directory, name + ".png");
        string details = Path.Combine(directory, name + ".json");

        // Bgr32 rather than Bgra32: a screen capture's alpha byte is meaningless,
        // and read as alpha it would save a transparent image.
        BitmapSource bitmap = BitmapSource.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr32, null, frame.Pixels, frame.Width * 4);
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using (FileStream file = File.Create(image))
        {
            encoder.Save(file);
        }

        var record = new
        {
            name,
            profile = display.Value.ProfileKey,
            width = display.Value.Width,
            height = display.Value.Height,
            uiScale = display.Value.UiScale,
            captured = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
        };

        File.WriteAllText(details, JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Saved {image} and {details} for profile {display.Value.ProfileKey}.");
        return 0;
    }
}
