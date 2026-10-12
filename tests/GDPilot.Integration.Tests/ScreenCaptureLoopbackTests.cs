using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using GDPilot.Vision;

namespace GDPilot.Integration.Tests;

[Collection(LoopbackCollection.Name)]
public sealed class ScreenCaptureLoopbackTests
{
    private readonly LoopbackFixture loopback;

    public ScreenCaptureLoopbackTests(LoopbackFixture loopback)
    {
        this.loopback = loopback;
    }

    /// <summary>
    /// Compares the capture with an independent GDI+ capture of the same region.
    /// Asserting the loopback window's own colour depended on nothing covering it,
    /// and another application's always-on-top window did cover it during runs.
    /// Two captures of whatever is on screen must agree, whatever that is, which
    /// still checks the size, the row order and the channel order.
    /// </summary>
    [Fact]
    public void CaptureAgreesWithAnIndependentGdiPlusCapture()
    {
        (int X, int Y, int Width, int Height) area = loopback.ClientBounds;
        Stopwatch clock = Stopwatch.StartNew();
        CapturedFrame frame = ScreenCapture.Region(area.X, area.Y, area.Width, area.Height);
        double matching = AgreementWithReference(frame, area.X, area.Y);

        // Anything animating on screen can change between the two captures, so a
        // mismatch is retried briefly before it counts.
        while (matching <= 0.95 && clock.Elapsed < TimeSpan.FromSeconds(2))
        {
            Thread.Sleep(50);
            frame = ScreenCapture.Region(area.X, area.Y, area.Width, area.Height);
            matching = AgreementWithReference(frame, area.X, area.Y);
        }

        Assert.Equal(area.Width * area.Height * 4, frame.Pixels.Length);
        Assert.Equal(area.Width, frame.Width);
        Assert.Equal(area.Height, frame.Height);

        // This test has failed intermittently for reasons not yet found, so a
        // failure reports what was actually on screen rather than only that it differed.
        if (matching <= 0.95)
        {
            string owner = ScreenOwner.Describe(area.X + area.Width / 2, area.Y + area.Height / 2);
            IntPtr handle = loopback.WindowHandle;
            Assert.Fail($"Only {matching:P1} of the captured pixels matched the reference capture after {clock.Elapsed.TotalSeconds:F1}s. "
                + $"Most common colour {DominantColour(frame)}. Window at the client centre: {owner}. Bounds {area}. "
                + $"Loopback window {handle}, topmost {ScreenOwner.IsTopmost(handle)}, raise succeeded {loopback.RaiseWindow()}.");
        }
    }

    private static string DominantColour(CapturedFrame frame)
    {
        Dictionary<int, int> counts = new();

        for (int offset = 0; offset < frame.Pixels.Length; offset += 4)
        {
            int colour = frame.Pixels[offset] | frame.Pixels[offset + 1] << 8 | frame.Pixels[offset + 2] << 16;
            counts[colour] = counts.GetValueOrDefault(colour) + 1;
        }

        KeyValuePair<int, int> top = counts.MaxBy(pair => pair.Value);
        double share = (double)top.Value / (frame.Width * frame.Height);
        return $"b={top.Key & 0xFF} g={top.Key >> 8 & 0xFF} r={top.Key >> 16 & 0xFF} ({share:P0})";
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 0)]
    [InlineData(-4, 10)]
    public void EmptyRegionsAreRejected(int width, int height)
    {
        Assert.Throws<ArgumentException>(() => ScreenCapture.Region(0, 0, width, height));
    }

    private static double AgreementWithReference(CapturedFrame frame, int x, int y)
    {
        using Bitmap reference = new(frame.Width, frame.Height, PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(reference))
        {
            graphics.CopyFromScreen(x, y, 0, 0, new Size(frame.Width, frame.Height));
        }

        int matched = 0;

        for (int row = 0; row < frame.Height; row++)
        {
            for (int column = 0; column < frame.Width; column++)
            {
                Color expected = reference.GetPixel(column, row);
                int offset = (row * frame.Width + column) * 4;

                if (frame.Pixels[offset] == expected.B && frame.Pixels[offset + 1] == expected.G && frame.Pixels[offset + 2] == expected.R)
                {
                    matched++;
                }
            }
        }

        return (double)matched / (frame.Width * frame.Height);
    }
}
