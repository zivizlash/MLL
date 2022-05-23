namespace MLL;

public static class ImageTools
{
    public static double[] RgbToGreyscale(byte[] pixels)
    {
        var greyscale = new double[pixels.Length / 3];

        for (int pi = 0, ri = 0; pi < pixels.Length; pi += 3, ri++)
        {
            var (r, g, b) = (pixels[pi], pixels[pi + 1], pixels[pi + 2]);
            greyscale[ri] = (r + g + b) / (255.0 * 3);
        }

        return greyscale;
    }

    // not tested
    public static void NormalizePixels(double[] pixels)
    {
        double min = 0, max = 0;

        foreach (var pixel in pixels)
            (min, max) = (Math.Min(min, pixel), Math.Max(max, pixel));
        
        double delta = 1.0 / Math.Min(1.0 - max, min);

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = (pixels[i] - max / 2) * delta + 0.5;
    }
}
