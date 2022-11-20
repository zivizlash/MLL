using System;

namespace MLL.Network.Tools;

public struct BufferLengthWrapper
{
    public byte[] Buffer;
    public int FilledSize;

    public bool IsFilled => Buffer.Length == FilledSize;
    public int EmptySize => Buffer.Length - FilledSize;

    public BufferLengthWrapper(byte[] buffer)
    {
        Buffer = buffer;
        FilledSize = 0;
    }

    public bool AddLength(int length)
    {
        var totalLength = FilledSize + length;

        if (totalLength > Buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        FilledSize = totalLength;
        return IsFilled;
    }

    public Memory<byte> AsFreeMemory() => Buffer.AsMemory(FilledSize, EmptySize);

    public void Reset() => FilledSize = 0;
}
