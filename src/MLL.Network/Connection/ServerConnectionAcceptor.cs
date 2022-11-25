using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MLL.Network.Message.Protocol;

public class ServerConnectionAcceptor : IDisposable
{
    private readonly ConcurrentDictionary<Guid, RemoteConnectionInfo> _clients;
    private readonly TcpListener _tcpListener;
    private readonly CancellationTokenSource _cancellationSource;
    private readonly IConnectionListener _connectionListener;

    private bool _disposed;

    public ServerConnectionAcceptor(IPEndPoint endpoint, IConnectionListener listener)
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

            var clientInfo = new RemoteConnectionInfo(Guid.NewGuid(), tcpClient, Disconnect);

            if (!await _connectionListener.OnConnectionVerifyAsync(clientInfo))
            {
                tcpClient.Close();
                continue;
            }

            if (!_clients.TryAdd(clientInfo.Uid, clientInfo))
            {
                throw new InvalidOperationException();
            }

            await _connectionListener.OnConnectedAsync(clientInfo);
        }
    }

    private async Task DisconnectInternal(RemoteConnectionInfo clientInfo)
    {
        // impossible
        if (!_clients.TryRemove(clientInfo.Uid, out _))
        {
            throw new InvalidOperationException("Client not found in internal collection.");
        }

        await _connectionListener.OnDisconnectedAsync(clientInfo);
    }

    internal ValueTask Disconnect(RemoteConnectionInfo clientInfo)
    {
        return clientInfo.TrySetClosingStatus()
            ? new ValueTask(DisconnectInternal(clientInfo))
            : new ValueTask();
    }

    private void CheckDisposed()
    {
        if (_disposed) ThrowDisposed();
    }

    private void ThrowDisposed()
    {
        throw new ObjectDisposedException(nameof(ServerConnectionAcceptor));
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
