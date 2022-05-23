using ImageMagick;

namespace MLL;

public class ImageLoader
{
    public static IEnumerable<ImageData> Load(string path)
    {
        foreach (var file in Directory.EnumerateFiles(path))
        {
            var filename = Path.GetFileNameWithoutExtension(file);

            if (!int.TryParse(filename, out var number) || number is < 0 or > 9)
                continue;

            using var imageStream = new FileStream(Path.Combine(file), FileMode.Open);
            using var image = new MagickImage(imageStream);
            
            var pixels = image.GetPixels().ToByteArray(PixelMapping.RGB)
                ?? throw new InvalidOperationException();
            
            yield return new ImageData(number, ImageTools.RgbToGreyscale(pixels));
        }
    }
}
