using MLL.Common.Tools;
using System.Collections.Concurrent;

namespace MLL.Common.Pooling;

public class Pool<T>
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

    internal bool ReplaceReturn(Pooled<T> pooled, bool throwIfAlreadyReturned, T value)
    {
        var isSuccessful = Return(pooled, throwIfAlreadyReturned);

        if (isSuccessful)
        {
            pooled.PoolItem.Value = value;
        }

        return isSuccessful;
    }

    internal bool Return(Pooled<T> pooled, bool throwIfAlreadyReturned)
    {
        var poolItem = pooled.PoolItem;

        if (pooled.Version != poolItem.CurrentVersion)
        {
            if (throwIfAlreadyReturned)
            {
                Throw.InvalidOperation("Object already in a pool");
            }

            return false;
        }

        poolItem.CurrentVersion++;
        _pool.Enqueue(poolItem);
        return true;
    }
}

public class PoolItem<T>
{
    public uint CurrentVersion;
    public T Value;

    public PoolItem(T value)
    {
        Value = value;
    }
}

public readonly struct Pooled<T> : IDisposable
{
    private readonly Pool<T> _pool;

#pragma warning disable IDE1006 // Naming Styles
    internal readonly uint Version;
    internal readonly PoolItem<T> PoolItem;
#pragma warning restore IDE1006 // Naming Styles

    public bool IsReturned => Version != PoolItem.CurrentVersion;

    public T Value
    {
        get
        {
            if (IsReturned)
            {
                Throw.InvalidOperation("Object returned to pool.");
            }

            return PoolItem.Value;
        }
    }

    public Pooled(PoolItem<T> value, Pool<T> pool)
    {
        _pool = pool;
        Version = value.CurrentVersion;
        PoolItem = value;
    }

    public void Return()
    {
        _pool.Return(this, true);
    }

    public void ReplaceReturn(T value)
    {
        _pool.ReplaceReturn(this, true, value);
    }

    public void Dispose()
    {
        _pool.Return(this, false);
    }
}
