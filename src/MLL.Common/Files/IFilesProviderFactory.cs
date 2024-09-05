namespace MLL.Common.Files;

public interface IFilesProviderFactory
{
    IFilesProvider Create(string folder);
}
