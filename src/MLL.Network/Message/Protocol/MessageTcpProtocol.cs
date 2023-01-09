using Microsoft.Extensions.Logging;
using MLL.Common.Pooling;
using MLL.Network.Message.Protocol.Exceptions;
using MLL.Network.Tools;
using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MLL.Network.Message.Protocol;

public readonly struct LoggerEnabled
{
    public readonly bool None;
    public readonly bool Trace;
    public readonly bool Debug;
    public readonly bool Information;
    public readonly bool Error;
    public readonly bool Critical;
    public readonly bool Warning;

    public LoggerEnabled(ILogger logger)
    {
        None = logger.IsEnabled(LogLevel.None);
        Trace = logger.IsEnabled(LogLevel.Trace);
        Debug = logger.IsEnabled(LogLevel.Debug);
        Information = logger.IsEnabled(LogLevel.Information);
        Error = logger.IsEnabled(LogLevel.Error);
        Warning = logger.IsEnabled(LogLevel.Warning);
        Critical = logger.IsEnabled(LogLevel.Critical);
    }
}

public class MessageTcpProtocol
{
    public static readonly int ContentLengthSize = Marshal.SizeOf<uint>();
    public static readonly int ContentTypeSize = Marshal.SizeOf<ushort>();
    public static readonly byte[] Magic = BitConverter.GetBytes((short)0x228);
    public static readonly int HeaderSize = Magic.Length + ContentLengthSize + ContentTypeSize;

    private readonly TcpConnectionInfo _connection;
    private readonly ILogger<MessageTcpProtocol> _logger;
    private readonly LoggerEnabled _loggerEnabled;
    private readonly CollectionPool<byte> _bytesPool;

    private const string _errorMessage = "Uid: {Uid}; ";

    public MessageTcpProtocol(TcpConnectionInfo connection, CollectionPool<byte> bytesPool, ILogger<MessageTcpProtocol> logger)
    {
        _connection = connection;
        _bytesPool = bytesPool;
        _logger = logger;
        _loggerEnabled = new(logger);
    }

    public async Task<RawMessage> ReadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await ReadMessageAsync(cancellationToken);
        }
        catch (InvalidMagicCodeException ex)
        {
            var message = "Protocol Error: Invalid Magic Code.";

            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, _errorMessage + message, _connection.Uid);
            }

            throw new ProtocolException(message, ex);
        }
        catch (OverflowException ex)
        {
            var message = "Internal error due casting UInt32 to Int32.";

            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, _errorMessage + message, _connection.Uid);
            }

            throw new ProtocolException(message, ex);
        }
        catch (OperationCanceledException ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, _errorMessage + "Operation canceled.", _connection.Uid);
            }

            throw;
        }
        catch (Exception ex)
        {
            var message = "Unknown exception";

            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogError(ex, _errorMessage + message, _connection.Uid);
            }

            throw new ProtocolException(message, ex);
        }
    }

    private async Task<RawMessage> ReadMessageAsync(CancellationToken token)
    {
        var networkStream = _connection.Client.GetStream();
        
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

        try
        {
            await networkStream.WriteAsync(Magic, token);
            await networkStream.WriteAsync(BitConverter.GetBytes((uint)data.Length), token);
            await networkStream.WriteAsync(BitConverter.GetBytes(contentType), token);
            await networkStream.WriteAsync(data, token);
            await networkStream.FlushAsync(token);
        }
        catch (OperationCanceledException ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {

            }

            throw;
        }
        catch (IOException ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {

            }

            throw;
        }
    }

    private static Task<BufferLengthWrapper> ReadInternalAsync(Stream stream, int length, CancellationToken token)
    {
        return ReadInternalAsync(stream, new BufferLengthWrapper(new byte[length]), token);
    }

    private static async Task<BufferLengthWrapper> ReadInternalAsync(Stream stream, BufferLengthWrapper buffer, CancellationToken token)
    {
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
    }

    private static void ThrowInvalidMagic() => throw new InvalidMagicCodeException("Incorrect magic code");
}
