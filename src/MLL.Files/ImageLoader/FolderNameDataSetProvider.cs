using MLL.Common.Files;

namespace MLL.Files.ImageLoader;

public class FolderNameDataSetProvider : IImageDataSetProvider
{
    private readonly IFilesProviderFactory _filesProviderFactory;
    private readonly Func<string, string> _dataSetNameToFolder;
    private readonly ImageDataSetOptions _options;
    private readonly Dictionary<string, IImageDataSet> _dataSets;
    private readonly Dictionary<string, string> _cachedFolders;

    public FolderNameDataSetProvider(IFilesProviderFactory filesProviderFactory,
        Func<string, string> dataSetNameToFolder, int width, int height)
    {
        _dataSets = new Dictionary<string, IImageDataSet>();
        _cachedFolders = new Dictionary<string, string>();
        _filesProviderFactory = filesProviderFactory;
        _dataSetNameToFolder = dataSetNameToFolder;
        _options = new ImageDataSetOptions(width, height);
    }

    public IImageDataSet GetDataSet(string name, object value)
    {
        if (!_cachedFolders.TryGetValue(name, out var folder))
            folder = _cachedFolders[name] = _dataSetNameToFolder.Invoke(name);

        if (string.IsNullOrEmpty(folder))
            throw new ArgumentOutOfRangeException(nameof(name));

        if (_dataSets.TryGetValue(folder, out var imageDataSet))
            return imageDataSet;

        var filesProvider = _filesProviderFactory.Create(folder);
        var imageData = new ImageDataSet(filesProvider, value, _options);
        return _dataSets[folder] = imageData;
    }

    public int GetLargestImageDataSetCount(IEnumerable<(string name, object value)> indices) =>
        indices.Select(t => GetDataSet(t.name, t.value).Count).Max();

    public void LoadAllImages(IEnumerable<(string name, object value)> indices)
    {
        foreach (var dataSet in _dataSets.Values)
            dataSet.EnsureAllImagesLoaded();
    }
}
