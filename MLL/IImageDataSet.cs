using System.Collections;
using ImageMagick;

namespace MLL;

public interface IImageDataSetProvider
{
    IImageDataSet GetDataSet(string name, object value);
}

public interface IImageDataSet
{
    ImageDataSetOptions Options { get; }
}

public class FolderImageDataSet : IImageDataSet, IEnumerable<ImageData>
{
    private readonly ImageData[] _images;

    public ImageDataSetOptions Options { get; }
    public object Value { get; }
    public int Length => _images.Length;

    public FolderImageDataSet(string folder, object value, ImageDataSetOptions options)
    {
        Options = options;
        Value = value ?? throw new ArgumentNullException(nameof(value));

        _images = Directory
            .EnumerateFiles(folder)
            .Select(file => FileToImage(file, options))
            .ToArray();
    }

    public ImageData FileToImage(string file, ImageDataSetOptions options)
    {
        var magickImage = new MagickImage(new FileInfo(file));
        magickImage.Resize(new MagickGeometry(options.Width, options.Height));

        var rgbPixels = magickImage.GetPixels().ToByteArray(PixelMapping.RGB)
            ?? throw new InvalidOperationException();
        
        return new ImageData(Value, ImageTools.RgbToGreyscale(rgbPixels));
    }

    public IEnumerator<ImageData> GetEnumerator() => 
        _images.AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class FolderNameDataSetProvider : IImageDataSetProvider
{
    private readonly Func<string, string> _dataSetNameToFolder;
    private readonly Dictionary<string, IImageDataSet> _dataSets;
    private readonly ImageDataSetOptions _options;

    public FolderNameDataSetProvider(
        Func<string, string> dataSetNameToFolder, ImageDataSetOptions options)
    {
        _dataSetNameToFolder = dataSetNameToFolder;
        _options = options;
        _dataSets = new Dictionary<string, IImageDataSet>();
    }

    public IImageDataSet GetDataSet(string name, object value)
    {
        var folder = _dataSetNameToFolder.Invoke(name);

        if (string.IsNullOrEmpty(folder))
            throw new ArgumentOutOfRangeException(nameof(name));

        if (_dataSets.TryGetValue(folder, out var imageDataSet))
            return imageDataSet;

        imageDataSet = new FolderImageDataSet(folder, value, _options);
        _dataSets[folder] = imageDataSet;
        return imageDataSet;
    }
}
