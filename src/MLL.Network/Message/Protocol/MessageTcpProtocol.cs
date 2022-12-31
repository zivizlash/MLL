using MLL.Network.Tools;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MLL.Network.Message.Protocol;

public class MessageTcpProtocol
{
    public static readonly int ContentLengthSize = Marshal.SizeOf<uint>();
    public static readonly int ContentTypeSize = Marshal.SizeOf<ushort>();
    public static readonly byte[] Magic = BitConverter.GetBytes((short)0x228);
    public static readonly int HeaderSize = Magic.Length + ContentLengthSize + ContentTypeSize;

    private readonly TcpConnectionInfo _connection;

    public MessageTcpProtocol(TcpConnectionInfo connection)
    {
        _connection = connection;
    }

    public async Task<RawMessage> ReadAsync(CancellationToken cancellationToken = default)
    {
        using var tokenBound = cancellationToken.CombineWithTimeout(_connection.RequestTimeout);
        var networkStream = _connection.Client.GetStream();

        var token = tokenBound.Token;

        var headerData = await ReadInternalAsync(networkStream, HeaderSize, token);
        var (contentLengthRaw, contentType) = ParseHeader(headerData);

        var contentLength = checked((int)contentLengthRaw);
        var content = await ReadInternalAsync(networkStream, contentLength, token);

        return new RawMessage
        {
            MessageType = contentType,
            Data = content.Buffer
        };
    }

    public async Task WriteAsync(byte[] data, ushort contentType, CancellationToken cancellationToken = default)
    {
        using var tokenBound = cancellationToken.CombineWithTimeout(_connection.RequestTimeout);
        var networkStream = _connection.Client.GetStream();

        var token = tokenBound.Token;

        await networkStream.WriteAsync(Magic, token);
        await networkStream.WriteAsync(BitConverter.GetBytes((uint)data.Length), token);
        await networkStream.WriteAsync(BitConverter.GetBytes(contentType), token);
        await networkStream.WriteAsync(data, token);
        await networkStream.FlushAsync(token);
    }

    private static async Task<BufferLengthWrapper> ReadInternalAsync(
        Stream stream, int length, CancellationToken token)
    {
        var arr = new byte[length];
        var buffer = new BufferLengthWrapper(arr);

        while (!buffer.IsFilled)
        {
            token.ThrowIfCancellationRequested();
            buffer.AddLength(await stream.ReadAsync(buffer.AsFreeMemory(), token));
        }

        return buffer;
    }

    private (uint contentLength, ushort contentType) ParseHeader(BufferLengthWrapper header)
    {
        var span = header.Buffer.AsSpan();
        CheckMagic(span[..Magic.Length]);

        int offset = Magic.Length;
        var contentLength = BitConverter.ToUInt32(span[offset..(offset + ContentLengthSize)]);

        offset += ContentLengthSize;
        var contentType = BitConverter.ToUInt16(span[offset..]);

        return (contentLength, contentType);
    }

    private static void CheckMagic(Span<byte> bytes)
    {
        for (int i = 0; i < Magic.Length; i++)
        {
            if (Magic[i] != bytes[i]) ThrowInvalidMagic();
        }
    }

    public void Stop()
    {
        _connection.Client.Dispose();
        //_connection.
    }

    private static void ThrowInvalidMagic() => throw new InvalidOperationException("Incorrect magic code");
}
