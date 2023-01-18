using MLL.Network.Exceptions;
using MLL.Network.Message.Listening;
using MLL.Network.Tools;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MLL.Network.Message.Protocol;

public class ClientListenerToMessageHandler : IConnectionListener
{
    private readonly IListenerMessageHandlerPipeFactory _pipeFactory;
    private readonly ConcurrentFlag _isWorking;
    private volatile ListenerMessageHandlerPipe? _pipe;

    public ClientListenerToMessageHandler(IListenerMessageHandlerPipeFactory pipeFactory)
    {
        _isWorking = new();
        _pipeFactory = pipeFactory;
    }

    public ValueTask OnConnectedAsync(RemoteConnectionInfo clientInfo)
    {
        if (!_isWorking.TrySetValue(true))
        {
            throw new InvalidOperationException("Client already connected to server.");
        }

        var factoryContext = new ListenerMessageHandlerPipeFactoryContext(clientInfo);
        _pipe = _pipeFactory.Create(factoryContext);
        return new ValueTask();
    }

    public ValueTask<bool> OnConnectionVerifyAsync(RemoteConnectionInfo clientInfo)
    {
        return new ValueTask<bool>(true);
    }

    public ValueTask OnDisconnectedAsync(RemoteConnectionInfo clientInfo)
    {
        if (!_isWorking.TrySetValue(false))
        {
            throw new InvalidOperationException("Client not connected to server.");
        }

        return _pipe?.StopAsync() ?? new ValueTask();
    }
}

public class ServerListenerToMessageHandler : IConnectionListener
{
    private readonly ConcurrentDictionary<Guid, Connection> _clients;
    private readonly IListenerMessageHandlerPipeFactory _pipeFactory;

    public ServerListenerToMessageHandler(IListenerMessageHandlerPipeFactory pipeFactory)
    {
        _clients = new();
        _pipeFactory = pipeFactory;
    }

    public ValueTask OnConnectedAsync(RemoteConnectionInfo clientInfo)
    {
        var connection = new Connection(clientInfo);

        if (!_clients.TryAdd(clientInfo.Uid, connection))
        {
            throw new InternalDictionaryInconsistentException(nameof(_clients));
        }

        try
        {
            var context = new ListenerMessageHandlerPipeFactoryContext(clientInfo);
            connection.Listener = _pipeFactory.Create(context); 

            // checking if we concurrently want to disconnect
            if (!_clients.ContainsKey(clientInfo.Uid))
            {
                return connection.Listener.StopAsync();
            }
        }
        catch
        {
            _clients.TryRemove(clientInfo.Uid, out _);
            throw;
        }

        return new ValueTask();
    }

    public ValueTask<bool> OnConnectionVerifyAsync(RemoteConnectionInfo clientInfo)
    {
        return new ValueTask<bool>(true);
    }

    public ValueTask OnDisconnectedAsync(RemoteConnectionInfo clientInfo)
    {
        Console.WriteLine("Disconnected");

        if (!_clients.TryRemove(clientInfo.Uid, out var connection))
        {
            throw new InternalDictionaryInconsistentException(nameof(_clients));
        }

        return connection.Listener?.StopAsync() ?? new ValueTask();
    }

    private class Connection
    {
        public readonly RemoteConnectionInfo ClientInfo;
        public volatile ListenerMessageHandlerPipe? Listener;

        public Connection(RemoteConnectionInfo clientInfo)
        {
            ClientInfo = clientInfo;
        }
    }
}
