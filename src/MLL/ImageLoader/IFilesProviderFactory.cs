namespace MLL.ImageLoader;

public interface IFilesProviderFactory
{
    IFilesProvider Create(string folder);
}
