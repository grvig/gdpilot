using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using GDPilot.Output;

namespace GDPilot.App;

/// <summary>
/// GDPilot's place beside the clock: pausing it, reaching its settings, whether it
/// starts with Windows, and quitting. Runs on a thread of its own, since a menu
/// must never hold up the loop that reads the pad.
/// </summary>
internal sealed class TrayIcon : IDisposable
{
    private readonly Thread thread;
    private readonly Action togglePause;
    private readonly Func<bool> paused;
    private readonly Action quit;
    private readonly string settingsFile;
    private readonly StartupEntry startup = new();
    private ApplicationContext? context;

    public TrayIcon(Action togglePause, Func<bool> paused, Action quit, string settingsFile)
    {
        this.togglePause = togglePause;
        this.paused = paused;
        this.quit = quit;
        this.settingsFile = settingsFile;
        thread = new Thread(Run) { IsBackground = true, Name = "Tray" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    private void Run()
    {
        using Bitmap picture = DrawPicture();
        IntPtr handle = picture.GetHicon();
        using Icon icon = Icon.FromHandle(handle);
        using ContextMenuStrip menu = new();
        ToolStripMenuItem pause = new("Pause");
        ToolStripMenuItem withWindows = new("Start with Windows");
        string executable = Environment.ProcessPath ?? "";

        pause.Click += (_, _) => togglePause();
        withWindows.Click += (_, _) =>
        {
            startup.Set(executable, !startup.IsOn(executable));
        };
        menu.Items.Add(pause);
        menu.Items.Add(new ToolStripMenuItem("Settings file", null, (_, _) => Open(settingsFile)));
        menu.Items.Add(new ToolStripMenuItem("Log folder", null, (_, _) => Open(ConfigFile.OwnFolder)));
        menu.Items.Add(withWindows);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Quit", null, (_, _) => quit()));

        // Read as the menu opens, since the kill switch can change the one and the person the other.
        menu.Opening += (_, _) =>
        {
            pause.Checked = paused();
            withWindows.Checked = startup.IsOn(executable);
        };

        using NotifyIcon tray = new() { Icon = icon, Text = "GDPilot", ContextMenuStrip = menu, Visible = true };
        context = new ApplicationContext();
        Application.Run(context);
        tray.Visible = false;
        DestroyIcon(handle);
    }

    /// <summary>A gold G on stone, in the overlay's colours, so no picture file has to travel with the program.</summary>
    private static Bitmap DrawPicture()
    {
        const int Side = 32;
        Bitmap picture = new(Side, Side);
        using Graphics graphics = Graphics.FromImage(picture);
        using SolidBrush stone = new(Color.FromArgb(38, 32, 26));
        using SolidBrush gold = new(Color.FromArgb(214, 170, 92));
        using Pen rim = new(Color.FromArgb(150, 112, 60), 2);
        using Font font = new("Book Antiqua", 19, FontStyle.Bold, GraphicsUnit.Pixel);
        using StringFormat centred = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        graphics.FillEllipse(stone, 1, 1, Side - 2, Side - 2);
        graphics.DrawEllipse(rim, 1, 1, Side - 3, Side - 3);
        graphics.DrawString("G", font, gold, new RectangleF(0, 0, Side, Side), centred);
        return picture;
    }

    private static void Open(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception problem)
        {
            Console.Error.WriteLine($"Could not open {path}: {problem.Message}");
        }
    }

    public void Dispose()
    {
        ApplicationContext? running = context;

        if (running is not null)
        {
            Application.Exit();
        }

        thread.Join(TimeSpan.FromSeconds(2));
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}
