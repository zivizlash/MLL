using MLL.Common.Files;

namespace MLL.ImageLoader;

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
