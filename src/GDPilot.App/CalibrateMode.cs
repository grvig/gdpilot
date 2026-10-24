using GDPilot.Core;
using GDPilot.Input;
using GDPilot.Output;
using GDPilot.Vision;

namespace GDPilot.App;

/// <summary>
/// Section 11's wizard, run as --calibrate GridName with the panel open in the
/// game: the right stick moves the cursor, A confirms, B goes back, and the
/// D-pad sets cell counts when the lines cannot be read. The result goes into
/// config.json in the working directory, under the profile for the live display.
/// With --at, the two corner cells are given as numbers instead, for setups
/// without a pad to hand; --counts then stands in for the D-pad.
/// </summary>
internal static class CalibrateMode
{
    private static readonly TimeSpan Period = TimeSpan.FromMilliseconds(4);

    internal static int Run(GDPilotConfig config, string gridName, int[]? corners, (int Columns, int Rows)? counts)
    {
        try
        {
            ConfigEditor.RequireModeName(gridName);
        }
        catch (ArgumentException problem)
        {
            Console.Error.WriteLine(problem.Message);
            return 2;
        }

        LiveDisplay? display = GameDisplay.Read(config);

        if (display is null)
        {
            Console.Error.WriteLine($"Could not find a running {config.GameProcess} window and its settings file.");
            return 1;
        }

        CalibrationWizard wizard = new(DetectOnScreen);
        Console.WriteLine($"Calibrating {gridName} for {display.Value.ProfileKey}. Open that panel in the game first.");

        if (corners is null)
        {
            if (!FollowPad(config, wizard))
            {
                return 1;
            }
        }
        else
        {
            FollowNumbers(wizard, corners, counts);
        }

        if (wizard.Step != WizardStep.Done)
        {
            Console.WriteLine(wizard.Prompt);
            return 1;
        }

        string path = Path.Combine(Environment.CurrentDirectory, "config.json");
        File.WriteAllText(path, ConfigEditor.WithGrid(StartingText(path), display.Value, gridName, wizard.Result!));
        Console.WriteLine($"Saved {gridName} into {path}: {DetectMode.Describe(wizard.Result!)}");
        return 0;
    }

    private static bool FollowPad(GDPilotConfig config, CalibrationWizard wizard)
    {
        using SdlGamepadSource pad = new();
        pad.Read();

        if (!pad.IsConnected)
        {
            Console.Error.WriteLine("No pad is connected.");
            return false;
        }

        MouseSender mouse = new();
        SendInputSink output = new();
        CursorMotion cursor = new(config.Mapping().Cursor);
        double deadzone = config.Input.Deadzone.Right;
        GamepadButtons held = GamepadButtons.None;
        string shown = "";

        while (wizard.Step != WizardStep.Done && wizard.Step != WizardStep.Cancelled)
        {
            if (wizard.Prompt != shown)
            {
                shown = wizard.Prompt;
                Console.WriteLine(shown);
            }

            GamepadSnapshot snapshot = pad.Read();
            GamepadButtons pressed = snapshot.Buttons & ~held;
            held = snapshot.Buttons;
            StickPosition stick = StickShaping.ApplyRadialDeadzone(snapshot.RightStick, StickPosition.Centre, deadzone);

            if (cursor.Step(stick, Period) is PointerMove move)
            {
                output.Send(move);
            }

            Respond(wizard, pressed, mouse.CursorPosition());
            Thread.Sleep(Period);
        }

        return true;
    }

    /// <summary>
    /// The same steps with the answers given up front. Counts are only used when
    /// the grid lines cannot be read; a detected grid is saved as found.
    /// </summary>
    private static void FollowNumbers(CalibrationWizard wizard, int[] corners, (int Columns, int Rows)? counts)
    {
        wizard.Confirm((corners[0], corners[1]));
        wizard.Confirm((corners[2], corners[3]));

        if (wizard.Step == WizardStep.CountCells)
        {
            if (counts is null)
            {
                Console.WriteLine("Grid lines could not be read there. Give the size with --counts columns rows.");
                return;
            }

            wizard.Adjust(counts.Value.Columns - wizard.Counts.Columns, counts.Value.Rows - wizard.Counts.Rows);
            wizard.Confirm((0, 0));
        }

        Console.WriteLine(wizard.Prompt);
        wizard.Confirm((0, 0));
    }

    private static void Respond(CalibrationWizard wizard, GamepadButtons pressed, (int X, int Y) position)
    {
        if ((pressed & GamepadButtons.A) != 0)
        {
            wizard.Confirm(position);
        }

        if ((pressed & GamepadButtons.B) != 0)
        {
            wizard.Back();
        }

        if ((pressed & GamepadButtons.DPadRight) != 0)
        {
            wizard.Adjust(1, 0);
        }

        if ((pressed & GamepadButtons.DPadLeft) != 0)
        {
            wizard.Adjust(-1, 0);
        }

        if ((pressed & GamepadButtons.DPadDown) != 0)
        {
            wizard.Adjust(0, 1);
        }

        if ((pressed & GamepadButtons.DPadUp) != 0)
        {
            wizard.Adjust(0, -1);
        }
    }

    /// <summary>Grids are kept in screen coordinates, the ones the cursor is moved to.</summary>
    private static GridSpec? DetectOnScreen(ScreenRect area)
    {
        int left = Math.Max(0, area.X);
        int top = Math.Max(0, area.Y);
        int width = area.Width - (left - area.X);
        int height = area.Height - (top - area.Y);
        CapturedFrame frame = ScreenCapture.Region(left, top, width, height);
        // Find takes its right and bottom edges inclusively.
        GridSpec? found = DetectMode.Find(frame, 0, 0, width - 1, height - 1);

        if (found is null)
        {
            return null;
        }

        return found with { OriginX = found.OriginX + left, OriginY = found.OriginY + top };
    }

    /// <summary>A first calibration starts from the shipped example, so its bindings come along.</summary>
    private static string StartingText(string path)
    {
        if (File.Exists(path))
        {
            return File.ReadAllText(path);
        }

        string example = Path.Combine(AppContext.BaseDirectory, "config.example.json");

        if (File.Exists(example))
        {
            return File.ReadAllText(example);
        }

        return "{}";
    }
}
