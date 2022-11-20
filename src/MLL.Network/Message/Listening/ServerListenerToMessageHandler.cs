using MLL.Network.Message.Listening;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MLL.Network.Message.Protocol;

public class ServerListenerToMessageHandler : IServerConnectionListener
{
    private readonly ConcurrentDictionary<Guid, Connection> _clients;
    private readonly IListenerMessageHandlerPipeFactory _pipeFactory;

    public ServerListenerToMessageHandler(IListenerMessageHandlerPipeFactory pipeFactory)
    {
        _clients = new();
        _pipeFactory = pipeFactory;
    }

    public ValueTask OnConnectedAsync(ClientConnectionInfo clientInfo)
    {
        var connection = new Connection(clientInfo);

        if (!_clients.TryAdd(clientInfo.Uid, connection))
        {
            throw new InvalidOperationException();
        }

        try
        {
            var context = new ListenerMessageHandlerPipeFactoryContext(clientInfo);
            connection.Listener = _pipeFactory.Create(context); 

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

    public ValueTask<bool> OnConnectionVerifyAsync(ClientConnectionInfo clientInfo)
    {
        return new ValueTask<bool>(true);
    }

    public ValueTask OnDisconnectedAsync(ClientConnectionInfo clientInfo)
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
        public volatile ListenerMessageHandlerPipe? Listener;

        public Connection(ClientConnectionInfo clientInfo)
        {
            ClientInfo = clientInfo;
        }
    }
}
