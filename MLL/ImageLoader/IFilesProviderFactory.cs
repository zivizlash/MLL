namespace MLL.ImageLoader;

public interface IFilesProviderFactory
{
    IFilesProvider Create(string folder);
}

public class NotOrEvenFilesProviderFactory : IFilesProviderFactory
{
    private readonly bool _isEven;

    public NotOrEvenFilesProviderFactory(bool isEven) => _isEven = isEven;

    public IFilesProvider Create(string folder) => new FolderFilteredFilesProvider(folder, CreateFilter(_isEven));

    private static Func<string, bool> CreateFilter(bool isEven)
    {
        var counter = isEven ? 0 : 1;
        return _ => counter++ % 2 == 0;
    }
}
