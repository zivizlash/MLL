namespace MLL.ImageLoader;

public interface IFilesProviderFactory
{
    IFilesProvider Create(string folder);
}

public class NotOrEvenFilesProviderFactory : IFilesProviderFactory
{
    private readonly bool _isEven;
    private readonly int? _maxFilesCount;

    public NotOrEvenFilesProviderFactory(bool isEven, int? maxFilesCount = default)
    {
        _isEven = isEven;
        _maxFilesCount = maxFilesCount;
    }

    public IFilesProvider Create(string folder)
    {
        var provider = new FolderFilteredFilesProvider(folder, CreateFilter(_isEven));

        return _maxFilesCount.HasValue
            ? new MaxFilesCountFilesProviderDecorator(provider, _maxFilesCount.Value)
            : provider;
    }

    private static Func<string, bool> CreateFilter(bool isEven)
    {
        bool even = isEven;
        return _ => even = !even;
    }
}
