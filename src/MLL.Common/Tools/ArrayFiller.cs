namespace MLL.Common.Tools;

public struct ArrayFiller
{
    private readonly int _length;

    public byte[] Buffer;
    public int FilledSize;

    public bool IsFilled => _length == FilledSize;
    public int EmptySize => _length - FilledSize;

    public ArrayFiller(byte[] buffer) : this(buffer, buffer.Length)
    {
    }

    public ArrayFiller(byte[] buffer, int length)
    {
        Buffer = buffer;
        FilledSize = 0;
        _length = length;
    }

    public bool AddLength(int addedLength)
    {
        var totalLength = FilledSize + addedLength;

        if (totalLength > _length)
        {
            ThrowOutOfRange(nameof(addedLength));
        }

        FilledSize = totalLength;
        return IsFilled;
    }

    public void Deconstruct(out byte[] buffer, out int offset, out int count)
    {
        buffer = Buffer;
        offset = FilledSize;
        count = EmptySize;
    }

    public Memory<byte> AsFreeMemory() => Buffer.AsMemory(FilledSize, EmptySize);

    public void Reset() => FilledSize = 0;

    private void ThrowOutOfRange(string paramName) => throw new ArgumentOutOfRangeException(paramName);
}
