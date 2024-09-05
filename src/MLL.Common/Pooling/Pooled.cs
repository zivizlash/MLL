using MLL.Common.Tools;

namespace MLL.Common.Pooling;

public static class PooledExtensions
{
    public static Span<T> AsSpan<T>(this Pooled<T[]> pooled, int length)
    {
        return pooled.Value.AsSpan(0, length);
    }

    public static ArrayFiller AsFiller(this Pooled<byte[]> pooled, int length)
    {
        return new ArrayFiller(pooled.Value, length);
    }

    public static void Clear(this Pooled<byte[]> pooled, int start)
    {
        var arr = pooled.Value;
        Array.Clear(arr, start, arr.Length - start);
    }
}

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
