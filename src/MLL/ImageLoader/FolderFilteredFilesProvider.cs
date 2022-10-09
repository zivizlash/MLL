using MLL.Common.Files;

namespace MLL.ImageLoader;

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
