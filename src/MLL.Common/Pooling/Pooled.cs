using MLL.Common.Tools;

namespace MLL.Common.Pooling;

public readonly struct Pooled<T> : IDisposable
{
    private readonly IPoolingSource<T> _pool;

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
                Throw.Disposed("Object returned to pool.");
            }

            return PoolItem.Value;
        }
    }

    public Pooled(PoolItem<T> value, IPoolingSource<T> pool)
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
