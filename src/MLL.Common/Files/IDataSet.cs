namespace MLL.Common.Files;

public interface IDataSet
{
    public float[] Value { get; }
    public int Count { get; }
    SetData this[int index] { get; }
}
