namespace MLL.Common.Pooling;

public interface IPoolingSource<T>
{
    bool ReplaceReturn(Pooled<T> pooled, bool throwIfAlreadyReturned, T value);
    bool Return(Pooled<T> pooled, bool throwIfAlreadyReturned);
}
