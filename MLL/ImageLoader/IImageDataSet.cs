namespace MLL.ImageLoader;

public interface IImageDataSetProvider
{
    IImageDataSet GetDataSet(string name, object value);
}

public interface IImageDataSet
{
    public object Value { get; }
    public int Count { get; }
    ImageDataSetOptions Options { get; }
    ImageData this[int index] { get; }
}

public class ImageDataSet : IImageDataSet
{
    private readonly IFilesProvider _filesProvider;
    private readonly Dictionary<int, ImageData> _indexToImage;

    public ImageDataSetOptions Options { get; }

    public object Value { get; }
    public int Count => _filesProvider.Count;

    public ImageData this[int index] => GetOrLoad(index);

    public ImageDataSet(IFilesProvider filesProvider, object value, ImageDataSetOptions options)
    {
        Options = options;
        Value = value ?? throw new ArgumentNullException(nameof(value));

        _indexToImage = new Dictionary<int, ImageData>();
        _filesProvider = filesProvider;
    }

    private ImageData GetOrLoad(int index)
    {
        if (_indexToImage.TryGetValue(index, out var imageData))
            return imageData;

        using var imageStream = _filesProvider.OpenStream(index, out _);

        var imageValue = ImageTools.LoadImageData(imageStream, Options);
        return _indexToImage[index] = new ImageData(Value, imageValue);
    }
}

public class FolderNameDataSetProvider : IImageDataSetProvider
{
    private readonly IFilesProviderFactory _filesProviderFactory;
    private readonly Func<string, string> _dataSetNameToFolder;
    private readonly ImageDataSetOptions _options;
    private readonly Dictionary<string, IImageDataSet> _dataSets;

    public FolderNameDataSetProvider(IFilesProviderFactory filesProviderFactory, 
        Func<string, string> dataSetNameToFolder, ImageDataSetOptions options)
    {
        _filesProviderFactory = filesProviderFactory;
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

        var filesProvider = _filesProviderFactory.Create(folder);
        var imageData = new ImageDataSet(filesProvider, value, _options);
        return _dataSets[folder] = imageData;
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
