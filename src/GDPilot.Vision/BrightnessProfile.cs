namespace GDPilot.Vision;

/// <summary>Average brightness along strips of a captured frame, the input grid detection works from.</summary>
public static class BrightnessProfile
{
    /// <summary>One value per column from left to right, each averaged over the rows from top to bottom.</summary>
    public static double[] Columns(CapturedFrame frame, int left, int right, int top, int bottom)
    {
        double[] profile = new double[right - left + 1];

        for (int x = left; x <= right; x++)
        {
            double sum = 0;

            for (int y = top; y <= bottom; y++)
            {
                sum += Brightness(frame, x, y);
            }

            profile[x - left] = sum / (bottom - top + 1);
        }

        return profile;
    }

    /// <summary>One value per row from top to bottom, each averaged over the columns from left to right.</summary>
    public static double[] Rows(CapturedFrame frame, int left, int right, int top, int bottom)
    {
        double[] profile = new double[bottom - top + 1];

        for (int y = top; y <= bottom; y++)
        {
            double sum = 0;

            for (int x = left; x <= right; x++)
            {
                sum += Brightness(frame, x, y);
            }

            profile[y - top] = sum / (right - left + 1);
        }

        return profile;
    }

    private static double Brightness(CapturedFrame frame, int x, int y)
    {
        int offset = (y * frame.Width + x) * 4;
        return (frame.Pixels[offset] + frame.Pixels[offset + 1] + frame.Pixels[offset + 2]) / 3.0;
    }
}
