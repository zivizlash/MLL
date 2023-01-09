using MLL.Common.Tools;
using System.Collections.Concurrent;

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
        var isSuccessful = ((IPoolingSource<T>)this).Return(pooled, throwIfAlreadyReturned);

        if (isSuccessful)
        {
            pooled.PoolItem.Value = value;
        }

        return isSuccessful;
    }

    bool IPoolingSource<T>.Return(Pooled<T> pooled, bool throwIfAlreadyReturned)
    {
        var poolItem = pooled.PoolItem;

        if (pooled.Version != poolItem.CurrentVersion)
        {
            if (throwIfAlreadyReturned)
            {
                Throw.Disposed("Object already in a pool");
            }

            return false;
        }

        poolItem.CurrentVersion++;
        _pool.Enqueue(poolItem);
        return true;
    }
}
