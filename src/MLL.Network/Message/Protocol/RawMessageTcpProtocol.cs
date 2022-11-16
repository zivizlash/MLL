using MessagePack;
using MessagePack.Resolvers;
using MLL.Network.Message.Converters;
using MLL.Network.Message.Handlers;
using MLL.Network.Tools;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace MLL.Network.Message.Protocol;

public interface IServerConnectionListener
{
    ValueTask OnConnected(ClientConnectionInfo clientInfo);
    ValueTask<bool> OnConnectionVerify(ClientConnectionInfo clientInfo);
    ValueTask OnDisconnected(ClientConnectionInfo clientInfo);
}

public interface IMessageHandlerFactory
{
    IMessageHandler CreateMessageHandler(MessageHandlerFactoryContext context);
}

public interface IMessageSender
{
    Task SendAsync<T>(T message, CancellationToken cancellationToken = default);
}

public readonly struct MessageHandlerFactoryContext
{
    public Guid Uid { get; }
    public IMessageSender MessageSender { get; }

    public MessageHandlerFactoryContext(IMessageSender messageSender, Guid uid)
    {
        MessageSender = messageSender;
        Uid = uid;
    }
}

public class ServerConnectionListenerMessageHandlerAdapter : IServerConnectionListener
{
    private readonly ConcurrentDictionary<Guid, Connection> _clients;
    private readonly IMessageHandlerFactory _handlerFactory;
    private readonly MessageConverter _messageConverter;

    public ServerConnectionListenerMessageHandlerAdapter(
        IMessageHandlerFactory handlerFactory, MessageConverter messageConverter)
    {
        _clients = new();
        _handlerFactory = handlerFactory;
        _messageConverter = messageConverter;
    }

    public ValueTask OnConnected(ClientConnectionInfo clientInfo)
    {
        var connection = new Connection(clientInfo);

        if (!_clients.TryAdd(clientInfo.Uid, connection))
        {
            throw new InvalidOperationException();
        }

        try
        {
            var protocol = new RawMessageTcpProtocol(new(clientInfo.Client));
            var sender = new ServerMessageSender(protocol, _messageConverter);

            var factoryContext = new MessageHandlerFactoryContext(sender, clientInfo.Uid);
            var handler = _handlerFactory.CreateMessageHandler(factoryContext);

            var listener = new Listener(handler, _messageConverter, protocol);
            connection.Listener = listener;

            // checking if we concurrently want to disconnect
            if (!_clients.ContainsKey(clientInfo.Uid))
            {
                connection.Listener.Stop();
            }
        }
        catch
        {
            _clients.TryRemove(clientInfo.Uid, out _);
            throw;
        }

        return new ValueTask();
    }

    public ValueTask<bool> OnConnectionVerify(ClientConnectionInfo clientInfo)
    {
        return new ValueTask<bool>(true);
    }

    public ValueTask OnDisconnected(ClientConnectionInfo clientInfo)
    {
        if (!_clients.TryRemove(clientInfo.Uid, out var connection))
        {
            throw new InvalidOperationException();
        }

        connection.Listener?.Stop();
        return new ValueTask();
    }

    private class Connection
    {
        public readonly ClientConnectionInfo ClientInfo;
        public Listener? Listener;

        public Connection(ClientConnectionInfo clientInfo)
        {
            ClientInfo = clientInfo;
        }
    }

    private class ServerMessageSender : IMessageSender
    {
        private readonly RawMessageTcpProtocol _protocol;
        private readonly MessageConverter _converter;
        
        public ServerMessageSender(RawMessageTcpProtocol protocol, MessageConverter converter)
        {
            _protocol = protocol;
            _converter = converter;   
        }

        public async Task SendAsync<T>(T message, CancellationToken cancellationToken)
        {
            var (messageBytes, messageType) = _converter.Serialize(message);
            await _protocol.WriteAsync(messageBytes, messageType, cancellationToken);
        }
    }

    private class Listener
    {
        private readonly CancellationTokenSource _tokenSource;
        private readonly IMessageHandler _messageHandler;
        private readonly MessageConverter _messageConverter;

        public Listener(IMessageHandler messageHandler, MessageConverter messageConverter, 
            RawMessageTcpProtocol protocol)
        {
            _messageHandler = messageHandler;
            _messageConverter = messageConverter;
            _tokenSource = new();

            Task.Run(async () =>
            {
                var token = _tokenSource.Token;

                while (!token.IsCancellationRequested)
                {
                    var msgBytes = await protocol.ReadAsync(token);
                    var message = messageConverter.Deserialize(msgBytes.Data, msgBytes.MessageType);
                    await _messageHandler.HandleAsync(message);
                }
            });
        }

        public void Stop()
        {
            _tokenSource.Cancel();
        }
    }
}

public class ServerConnectionManager : IDisposable
{
    private readonly ConcurrentDictionary<Guid, ClientConnectionInfo> _clients;
    private readonly TcpListener _tcpListener;
    private readonly CancellationTokenSource _cancellationSource;
    private readonly IServerConnectionListener _connectionListener;

    private bool _disposed;

    public ServerConnectionManager(IPEndPoint endpoint, IServerConnectionListener listener)
    {
        _clients = new();
        _tcpListener = new(endpoint);
        _tcpListener.Start();
        _cancellationSource = new();
        _connectionListener = listener;

        Task.Run(ListenClientConnectionsAsync);
    }

    private async Task ListenClientConnectionsAsync()
    {
        var token = _cancellationSource.Token;

        while (!token.IsCancellationRequested)
        {
            TcpClient tcpClient;

            try
            {
                tcpClient = await _tcpListener.AcceptTcpClientAsync();
            }
            catch (SocketException se) when (se.SocketErrorCode == SocketError.OperationAborted)
            {
                break;
            }

            var clientInfo = new ClientConnectionInfo(Guid.NewGuid(), tcpClient, Disconnect);

            if (!await _connectionListener.OnConnectionVerify(clientInfo))
            {
                tcpClient.Close();
                continue;
            }

            if (!_clients.TryAdd(clientInfo.Uid, clientInfo))
            {
                throw new InvalidOperationException();
            }

            await _connectionListener.OnConnected(clientInfo);
        }
    }

    private async Task DisconnectInternal(ClientConnectionInfo clientInfo)
    {
        // impossible
        if (!_clients.TryRemove(clientInfo.Uid, out _))
        {
            throw new InvalidOperationException(
                "Client not found in internal collection");
        }

        await _connectionListener.OnDisconnected(clientInfo);
    }

    internal ValueTask Disconnect(ClientConnectionInfo clientInfo)
    {
        return clientInfo.TrySetClosingStatus()
            ? new ValueTask(DisconnectInternal(clientInfo))
            : new ValueTask();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cancellationSource.Cancel();

        foreach (var clientInfo in _clients)
        {
            // тут стопудова чота не так с дизайном, так не должно быть. надо подумать как лучше
            try
            {
                Disconnect(clientInfo.Value);
            }
            catch { }
        }

        try
        {
            _tcpListener.Stop();
        }
        catch { }
    }
}

public class ClientConnectionInfo
{
    private readonly Func<ClientConnectionInfo, ValueTask> _disconnect;

    private volatile int _status;

    private const int StatusWorking = 0;
    private const int StatusClosing = 1;

    public Guid Uid { get; set; }
    public TcpClient Client { get; set; }

    internal bool IsClosing => _status == StatusClosing;

    internal bool TrySetClosingStatus()
    {
        // we can starting disconnecting clients
        // while client already started disconnecting.

        var original = Interlocked.CompareExchange(ref _status, StatusClosing, StatusWorking);
        return original == StatusWorking;
    }

    internal ClientConnectionInfo(Guid uid, TcpClient client, 
        Func<ClientConnectionInfo, ValueTask> disconnect)
    {
        Uid = uid;
        Client = client;
        _disconnect = disconnect;
    }

    public ValueTask Disconnect()
    {
        return _disconnect.Invoke(this);
    }
}

public class RawMessageTcpProtocol
{
    public static readonly int ContentLengthSize = Marshal.SizeOf<uint>();
    public static readonly int ContentTypeSize = Marshal.SizeOf<ushort>();
    public static readonly byte[] Magic = BitConverter.GetBytes((short)0x228);
    public static readonly int HeaderSize = Magic.Length + ContentLengthSize + ContentTypeSize;

    private readonly TcpConnectionInfo _connection;

    static RawMessageTcpProtocol()
    {
        var options = MessagePackSerializer.DefaultOptions.WithResolver(
            ContractlessStandardResolver.Instance);

        MessagePackSerializer.DefaultOptions = options;
    }

    public RawMessageTcpProtocol(TcpConnectionInfo connection)
    {
        _connection = connection;
    }

    private static void CheckMagic(Span<byte> bytes)
    {
        for (int i = 0; i < Magic.Length; i++)
        {
            if (Magic[i] != bytes[i])
            {
                throw new InvalidOperationException("Incorrect magic code");
            }
        }
    }

    private static async Task<BufferLengthWrapper> ReadAsync(Stream stream, int length, CancellationToken token)
    {
        var arr = new byte[length];
        var buffer = new BufferLengthWrapper(arr);

        while (!buffer.IsFilled)
        {
            token.ThrowIfCancellationRequested();
            var memory = buffer.Buffer.AsMemory(buffer.FilledSize, buffer.EmptySize);
            buffer.AddLength(await stream.ReadAsync(memory, token));
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

    public async Task<RawMessageInfo> ReadAsync(CancellationToken cancellationToken = default)
    {
        using var tokenBound = cancellationToken.CombineWithTimeout(_connection.RequestTimeout);
        var networkStream = _connection.Client.GetStream();

        var token = tokenBound.Token;

        var headerData = await ReadAsync(networkStream, HeaderSize, token);
        var (contentLengthRaw, contentType) = ParseHeader(headerData);

        var contentLength = checked((int)contentLengthRaw);
        var content = await ReadAsync(networkStream, contentLength, token);

        return new RawMessageInfo
        {
            MessageType = contentType,
            Data = content.Buffer
        };
    }
}
