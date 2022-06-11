namespace MLL.ImageLoader;

public interface IFilesProvider
{
    int Count { get; }
    Stream OpenStream(int index, out string filename);
}

public class MaxFilesCountFilesProviderDecorator : IFilesProvider
{
    private readonly IFilesProvider _filesProvider;
    private readonly int _maxFilesCount;

    public int Count => Math.Min(_filesProvider.Count, _maxFilesCount);

    public MaxFilesCountFilesProviderDecorator(IFilesProvider filesProvider, int maxFilesCount)
    {
        _filesProvider = filesProvider;
        _maxFilesCount = maxFilesCount;
    }

    public Stream OpenStream(int index, out string filename) => 
        _filesProvider.OpenStream(index, out filename);
}

public class FolderFilteredFilesProvider : IFilesProvider
{
    private readonly string[] _files;

    public int Count => _files.Length;

    public FolderFilteredFilesProvider(string folder, Func<string, bool> filenameSelector, string ext = ".png")
    {
        _files = Directory
            .EnumerateFiles(folder)
            .Where(filename => Path.GetExtension(filename) == ext)
            .Where(filenameSelector)
            .ToArray();
    }

    public Stream OpenStream(int index, out string filename)
    {
        filename = _files[index];
        return File.OpenRead(filename);
    }
}
