namespace MLL.Repository.Data;

public interface INetData
{
    T? GetOrDefault<T>() where T : class;
    void Set<T>(T value) where T : class;
}
