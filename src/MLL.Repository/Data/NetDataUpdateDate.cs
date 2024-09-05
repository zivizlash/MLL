using MLL.Repository.DataModels;

namespace MLL.Repository.Data;

public class NetDataUpdateDate : INetData
{
    private readonly INetData _netData;

    public NetDataUpdateDate(INetData netData)
    {
        _netData = netData;
    }

    public T? GetOrDefault<T>() where T : class
    {
        return _netData.GetOrDefault<T>();
    }

    public void Set<T>(T value) where T : class
    {
        _netData.Set(value);
        _netData.Set(new NetInfoLastUpdateData());
    }
}
