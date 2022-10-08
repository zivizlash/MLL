namespace MLL.ImageLoader;

public interface IFilesProvider
{
    int Count { get; }
    Stream OpenStream(int index, out string filename);
}
