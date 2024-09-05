using MLL.Repository.Tools;

namespace MLL.Repository.Data;

public class NetDataCache : INetData
{
    private readonly INetData _netData;
    private readonly Dictionary<Type, object?> _cache;

    public NetDataCache(INetData netData)
    {
        _netData = netData;
        _cache = new();
    }

    public T? GetOrDefault<T>() where T : class
    {
        if (_cache.TryGetValue(ModelData<T>.Type, out var cached))
        {
            return (T?)cached;
        }

        var value = _netData.GetOrDefault<T>();
        _cache[ModelData<T>.Type] = value;
        return value;
    }

    public void Set<T>(T value) where T : class
    {
        _netData.Set(value);
        _cache[ModelData<T>.Type] = value;
    }
}
