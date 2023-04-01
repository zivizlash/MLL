using MLL.Repository.Data;

namespace MLL.Repository.Tools;

public static class NetDataExtensions
{
    public static T Get<T>(this INetData netData) where T : class =>
        netData.GetOrDefault<T>() ?? throw new KeyNotFoundException();

    public static T GetOrDefault<T>(this INetData netData, T defaultValue) where T : class =>
        netData.GetOrDefault<T>() ?? defaultValue;

    public static T GetOrDefault<T>(this INetData netData, Func<T> defaultFactory) where T : class =>
        netData.GetOrDefault<T>() ?? defaultFactory.Invoke();

    public static T GetOrNew<T>(this INetData netData) where T : class, new() =>
        netData.GetOrDefault<T>() ?? new T();

    public static bool Has<T>(this INetData netData) where T : class =>
        netData.GetOrDefault<T>() != null;

    public static void NewOrUpdate<T>(this INetData netData, Action<T> action)
        where T : class, new()
    {
        var data = netData.GetOrNew<T>();
        action.Invoke(data);
        netData.Set(data);
    }

    public static INetData WithCache(this INetData netData) =>
        new NetDataCache(netData);

    public static INetData WithUpdateDateTime(this INetData netData) =>
        new NetDataUpdateDate(netData);

    public static INetData WithCacheAndDateTimeUpdate(this INetData netData) =>
        netData.WithCache().WithUpdateDateTime();
}
