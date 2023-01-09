using System.Collections.Concurrent;

namespace MLL.Common.Pooling;

public class CollectionPool<T>
{
    private readonly ConcurrentDictionary<int, Pool<T[]>> _blocksPool;
    private readonly int _blockSize;
    private readonly Func<int, Pool<T[]>> _factory;

    public CollectionPool(int blockSize)
    {
        _blockSize = blockSize;
        _blocksPool = new();
        _factory = block => new Pool<T[]>(() => new T[block * blockSize]);
    }

    public Pooled<T[]> Get(int size)
    {
        var block = size / _blockSize + (size % _blockSize > 0 ? 1 : 0);
        return _blocksPool.GetOrAdd(block, _factory).Get();
    }
}
