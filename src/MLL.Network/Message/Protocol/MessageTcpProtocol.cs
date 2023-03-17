using MLL.Common.Pooling;
using MLL.Common.Tools;
using MLL.Network.Message.Protocol.Exceptions;
using MLL.Network.Tools;
using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MLL.Network.Message.Protocol;

public interface IMessageProtocol
{
    Task<RawMessage> ReadAsync(CancellationToken cancellationToken = default);
    Task WriteAsync(byte[] data, ushort contentType, CancellationToken cancellationToken = default);
}

public readonly struct SocketConnectionInfo
{
    public Guid Uid { get; }
    public Socket Socket { get; }
    public TimeSpan RequestTimeout { get; }

    public SocketConnectionInfo(Guid uid, Socket socket, TimeSpan requestTimeout)
    {
        Uid = uid;
        Socket = socket;
        RequestTimeout = requestTimeout;
    }
}

//public class SocketMessageTcpProtocol : IMessageProtocol
//{
//    public static readonly int ContentLengthSize = Marshal.SizeOf<uint>();
//    public static readonly int ContentTypeSize = Marshal.SizeOf<ushort>();
//    public static readonly byte[] Magic = BitConverter.GetBytes((short)0x228);
//    public static readonly int HeaderSize = Magic.Length + ContentLengthSize + ContentTypeSize;
//    public static readonly int MaxContentLength = 1024 * 1024 * 8;

//    private readonly SocketConnectionInfo _connection;
//    private readonly CollectionPool<byte> _dataPool;
//    private readonly CollectionPool<byte> _internalPool;

//    public SocketMessageTcpProtocol(SocketConnectionInfo connection,
//        CollectionPool<byte> dataPool, CollectionPool<byte> internalPool)
//    {
//        _connection = connection;
//        _dataPool = dataPool;
//        _internalPool = internalPool;
//    }

//    public async Task<RawMessage> ReadAsync(CancellationToken cancellationToken = default)
//    {
//        try
//        {
//            return await ReadMessageAsync(cancellationToken);
//        }
//        catch (InvalidMagicCodeException ex)
//        {
//            throw new ProtocolException("Invalid magic code. See inner exception.", ex);
//        }
//        catch (InvalidHeadersException ex)
//        {
//            throw new ProtocolException("Invalid headers. See inner exception.", ex);
//        }
//        catch (OverflowException ex)
//        {
//            throw new ProtocolException("Internal error due casting UInt32 to Int32.", ex);
//        }
//        catch (ObjectDisposedException ex)
//        {
//            throw new ProtocolException("Client closed.", ex);
//        }
//    }

//    private async Task<RawMessage> ReadMessageAsync(CancellationToken token)
//    {
//        uint contentLengthRaw;
//        ushort contentType;

//        using (var headerPooled = _internalPool.Get(HeaderSize))
//        {
//            Array.Clear(headerPooled.Value, 0, headerPooled.Value.Length);

//            var headerBuffer = headerPooled.AsFiller(HeaderSize);
//            await ReadInternalAsync(_connection.Socket, headerBuffer, token);
//            (contentLengthRaw, contentType) = ParseHeader(headerPooled.AsSpan(HeaderSize));
//        }

//        var contentLength = checked((int)contentLengthRaw);
//        var contentPooled = _dataPool.Get(contentLength);
//        var contentBuffer = contentPooled.AsFiller(contentLength);

//        try
//        {
//            await ReadInternalAsync(_connection.Socket, contentBuffer, token);
//        }
//        catch
//        {
//            contentPooled.Dispose();
//            throw;
//        }

//        return new RawMessage
//        {
//            MessageType = contentType,
//            Data = contentPooled,
//            Length = contentLength
//        };
//    }

//    public async Task WriteAsync(byte[] data, ushort contentType, CancellationToken cancellationToken = default)
//    {
//        using var tokenBound = cancellationToken.CombineWithTimeout(_connection.RequestTimeout);
        
//        var token = tokenBound.Token;
//        var socket = _connection.Socket;

//        try
//        {
//            await socket.SendAsync(Magic, SocketFlags.None, token);
//            await socket.SendAsync(BitConverter.GetBytes((uint)data.Length), SocketFlags.None, token);
//            await socket.SendAsync(BitConverter.GetBytes(contentType), SocketFlags.None, token);
//            await socket.SendAsync(data, SocketFlags.None, token);
//        }
//        catch (SocketException ex)
//        {
//            throw new ProtocolException("Socket error. See inner exception.", ex);
//        }
//        catch (ObjectDisposedException ex)
//        {
//            throw new ProtocolException("Client closed.", ex);
//        }
//    }

//    private static async Task<ArrayFiller> ReadInternalAsync(Socket socket, int length, CancellationToken token)
//    {
//        var buffer = new ArrayFiller(new byte[length]);
//        await ReadInternalAsync(socket, buffer, token);
//        return buffer;
//    }

//    private static async Task ReadInternalAsync(Socket socket, ArrayFiller buffer, CancellationToken token)
//    {
//        while (!buffer.IsFilled)
//        {
//            token.ThrowIfCancellationRequested();
//            var readedLength = await socket.ReceiveAsync(buffer.AsFreeMemory(), SocketFlags.None, token);

//            if (readedLength == 0)
//            {
//                throw new OperationCanceledException("NetworkStream ended.");
//            }

//            buffer.AddLength(readedLength);
//        }
//    }

//    private (uint contentLength, ushort contentType) ParseHeader(Span<byte> header)
//    {
//        CheckMagic(header[..Magic.Length]);

//        int offset = Magic.Length;
//        var contentLength = BitConverter.ToUInt32(header[offset..(offset + ContentLengthSize)]);

//        if (contentLength == 0 || contentLength > MaxContentLength)
//        {
//            ThrowInvalidHeaders("Content length must be greater than 1 byte and less than 8 megabytes.");
//        }

//        offset += ContentLengthSize;
//        var contentType = BitConverter.ToUInt16(header[offset..]);

//        return (contentLength, contentType);
//    }

//    private static void CheckMagic(Span<byte> bytes)
//    {
//        for (int i = 0; i < Magic.Length; i++)
//        {
//            if (Magic[i] != bytes[i]) ThrowInvalidMagic();
//        }
//    }

//    private static void ThrowInvalidHeaders(string message) => throw new InvalidHeadersException(message);
//    private static void ThrowInvalidMagic() => throw new InvalidMagicCodeException("Incorrect magic code");
//}

public class MessageTcpProtocol : IMessageProtocol
{
    public static readonly int ContentLengthSize = Marshal.SizeOf<uint>();
    public static readonly int ContentTypeSize = Marshal.SizeOf<ushort>();
    public static readonly byte[] Magic = BitConverter.GetBytes((short)0x228);
    public static readonly int HeaderSize = Magic.Length + ContentLengthSize + ContentTypeSize;
    public static readonly int MaxContentLength = 1024 * 1024 * 8;
     
    private readonly SocketConnectionInfo _connection;
    private readonly CollectionPool<byte> _dataPool;
    private readonly CollectionPool<byte> _internalPool;

    public MessageTcpProtocol(SocketConnectionInfo connection, 
        CollectionPool<byte> dataPool, CollectionPool<byte> internalPool)
    {
        _connection = connection;
        _dataPool = dataPool;
        _internalPool = internalPool;
    }

    public async Task<RawMessage> ReadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await ReadMessageAsync(cancellationToken);
        }
        catch (InvalidMagicCodeException ex)
        {
            throw new ProtocolException("Invalid magic code. See inner exception.", ex);
        }
        catch (InvalidHeadersException ex)
        {
            throw new ProtocolException("Invalid headers. See inner exception.", ex);
        }
        catch (OverflowException ex)
        {
            throw new ProtocolException("Internal error due casting UInt32 to Int32.", ex);
        }
        catch (ObjectDisposedException ex)
        {
            throw new ProtocolException("Client closed.", ex);
        }
    }

    private async Task<RawMessage> ReadMessageAsync(CancellationToken token)
    {
        var socket = _connection.Socket;

        uint contentLengthRaw;
        ushort contentType;

        using (var headerPooled = _internalPool.Get(HeaderSize))
        {
            Array.Clear(headerPooled.Value, 0, headerPooled.Value.Length);

            var headerBuffer = headerPooled.AsFiller(HeaderSize);
            await ReadInternalAsync(socket, headerBuffer, token);
            (contentLengthRaw, contentType) = ParseHeader(headerPooled.AsSpan(HeaderSize));
        }

        var contentLength = checked((int)contentLengthRaw);
        var contentPooled = _dataPool.Get(contentLength);
        var contentBuffer = contentPooled.AsFiller(contentLength);

        try
        {
            await ReadInternalAsync(socket, contentBuffer, token);
            //contentPooled.Clear(contentLength);
        }
        catch
        {
            contentPooled.Dispose();
            throw;
        }

        return new RawMessage
        {
            MessageType = contentType,
            Data = contentPooled,
            Length = contentLength
        };
    }

    public async Task WriteAsync(byte[] data, ushort contentType, CancellationToken cancellationToken = default)
    {
        using var tokenBound = cancellationToken.CombineWithTimeout(_connection.RequestTimeout);
        
        var token = tokenBound.Token;
        var socket = _connection.Socket;

        try
        {
            var dataBytes = BitConverter.GetBytes((uint)data.Length);
            var contentTypeBytes = BitConverter.GetBytes(contentType);

            await SendInternalAsync(socket, Magic, token);
            await SendInternalAsync(socket, dataBytes, token);
            await SendInternalAsync(socket, contentTypeBytes, token);
            await SendInternalAsync(socket, data, token);
        }
        catch (ObjectDisposedException ex)
        {   
            throw new ProtocolException("Client closed.", ex);
        }
    }

    private async Task SendInternalAsync(Socket socket, byte[] arr, CancellationToken token)
    {
        var total = 0;

        while (total < arr.Length)
        {
            var sent = await socket.SendAsync(arr.AsMemory(total), SocketFlags.None, token);
            total += sent;
        }
    }

    private static async Task ReadInternalAsync(Socket socket, ArrayFiller buffer, CancellationToken token)
    {
        while (!buffer.IsFilled)
        {
            token.ThrowIfCancellationRequested();

            var receivedCount = await socket.ReceiveAsync(buffer.AsFreeMemory(), SocketFlags.None);

            if (receivedCount == 0)
            {
                throw new OperationCanceledException("Connection forcebily closed.");
            }

            buffer.AddLength(receivedCount);
        }
    }

    private (uint contentLength, ushort contentType) ParseHeader(Span<byte> header)
    {
        CheckMagic(header[..Magic.Length]);

        int offset = Magic.Length;
        var contentLength = BitConverter.ToUInt32(header[offset..(offset + ContentLengthSize)]);

        if (contentLength == 0 || contentLength > MaxContentLength)
        {
            ThrowInvalidHeaders("Content length must be greater than 1 byte and less than 8 megabytes.");
        }

        offset += ContentLengthSize;
        var contentType = BitConverter.ToUInt16(header[offset..]);

        return (contentLength, contentType);
    }

    private static void CheckMagic(Span<byte> bytes)
    {
        for (int i = 0; i < Magic.Length; i++)
        {
            if (Magic[i] != bytes[i]) ThrowInvalidMagic();
        }
    }

    private static void ThrowInvalidHeaders(string message) => throw new InvalidHeadersException(message);
    private static void ThrowInvalidMagic() => throw new InvalidMagicCodeException("Incorrect magic code");
}
