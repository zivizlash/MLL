using System.Collections;
using ImageMagick;

namespace MLL;

public interface IImageDataSetProvider
{
    IImageDataSet GetDataSet(string name, object value);
}

public interface IImageDataSet //: IEnumerable<ImageData>
{
    public object Value { get; }
    public int Length { get; }
    ImageDataSetOptions Options { get; }
    ImageData this[int index] { get; }
}

public class FolderImageDataSet : IImageDataSet
{
    private readonly List<string> _files;
    private readonly Dictionary<string, ImageData> _fileToImage;

    public ImageDataSetOptions Options { get; }

    public object Value { get; }
    public int Length => _files.Count;

    public ImageData this[int index] => GetOrLoad(index);

    public FolderImageDataSet(string folder, object value, ImageDataSetOptions options)
    {
        Options = options;
        Value = value ?? throw new ArgumentNullException(nameof(value));

        _fileToImage = new Dictionary<string, ImageData>();
        _files = Directory.EnumerateFiles(folder).ToList();
    }

    private ImageData GetOrLoad(int index)
    {
        var filename = _files[index];

        if (_fileToImage.TryGetValue(filename, out var imageData))
            return imageData;

        return _fileToImage[filename] = ImageTools.LoadImageData(Value, filename, Options);
    }
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

public static class ImageDataSetProviderExtensions
{
    private static readonly Dictionary<int, (string, object)> Cache;

    static ImageDataSetProviderExtensions() => 
        Cache = new Dictionary<int, (string, object)>();

    public static IImageDataSet GetDataSet(this IImageDataSetProvider provider, int value)
    {
        if (!Cache.ContainsKey(value)) 
            Cache[value] = (value.ToString(), value);

        var (name, objValue) = Cache[value];
        return provider.GetDataSet(name, objValue);
    }
}
