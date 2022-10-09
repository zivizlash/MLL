namespace MLL.Common.Files;

public interface IFilesProvider
{
    int Count { get; }
    Stream OpenStream(int index, out string filename);
}
