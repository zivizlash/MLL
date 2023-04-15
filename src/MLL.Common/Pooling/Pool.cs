using MLL.Common.Tools;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace MLL.Common.Pooling;

public class PoolItem<T>
{
    public volatile uint CurrentVersion;
    public T Value;

    public PoolItem(T value)
    {
        Value = value;
    }
}

public class Pool<T> : IPoolingSource<T>
{
    private readonly ConcurrentQueue<PoolItem<T>> _pool;
    private readonly Func<T> _factory;

    public Pool(Func<T> factory)
    {
        _pool = new();
        _factory = factory;
    }

    public Pooled<T> Get()
    {
        if (_pool.TryDequeue(out var item))
        {
            return new Pooled<T>(item, this);
        }

        return new Pooled<T>(new PoolItem<T>(_factory.Invoke()), this);
    }

    bool IPoolingSource<T>.ReplaceReturn(Pooled<T> pooled, bool throwIfAlreadyReturned, T value)
    {
        bool isCanReturn = CanReturnToPool(pooled, throwIfAlreadyReturned);

        if (isCanReturn)
        {
            pooled.PoolItem.Value = value;
            ReturnToPoolInternal(pooled);
        }

        return isCanReturn;
    }

    bool IPoolingSource<T>.Return(Pooled<T> pooled, bool throwIfAlreadyReturned)
    {
        bool isCanReturn = CanReturnToPool(pooled, throwIfAlreadyReturned);

        if (isCanReturn)
        {
            ReturnToPoolInternal(pooled);
        }

        return isCanReturn;
    }

    private static bool CanReturnToPool(Pooled<T> pooled, bool throwIfAlreadyReturned)
    {
        bool isPooled = pooled.Version != pooled.PoolItem.CurrentVersion;

        if (isPooled && throwIfAlreadyReturned)
        {
            Throw.Disposed("Object already in a pool");
        }

        return !isPooled;
    }

    private void ReturnToPoolInternal(Pooled<T> pooled)
    {
        pooled.PoolItem.CurrentVersion++;
        _pool.Enqueue(pooled.PoolItem);
    }
}
