using Microsoft.Extensions.Logging;
using MLL.Network.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MLL.Network.Message.Protocol;

public enum ConnectionState
{
    Connecting,
    Connected,
    Disconnecting,
    Unknown
}

public readonly struct IPConnectionInfo
{
    public ConnectionState State { get; }
    public TcpState InternalState { get; }
    public IPEndPoint RemoteEndPoint { get; }

    public IPConnectionInfo(ConnectionState state, TcpState internalState, IPEndPoint remote)
    {
        State = state;
        InternalState = internalState;
        RemoteEndPoint = remote;
    }
}

public class IPGlobal
{
    public static IEnumerable<IPConnectionInfo> GetConnectionsInfo()
    {
        var properties = IPGlobalProperties.GetIPGlobalProperties();
        
        foreach (var connection in properties.GetActiveTcpConnections())
        {
            ConnectionState state;

            switch (connection.State)
            {
                case TcpState.Closed:
                case TcpState.CloseWait:
                case TcpState.Closing:
                case TcpState.FinWait1:
                case TcpState.FinWait2:
                case TcpState.LastAck:
                    state = ConnectionState.Disconnecting;
                    break;
                case TcpState.Listen:
                case TcpState.SynReceived:
                case TcpState.SynSent:
                    state = ConnectionState.Connecting;
                    break;
                case TcpState.Established:
                    state = ConnectionState.Connected;
                    break;
                default:
                    state = ConnectionState.Unknown;
                    break;
            }

            yield return new IPConnectionInfo(state, connection.State, connection.RemoteEndPoint);
        }
    }
}

public class ServerConnectionAcceptor : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, RemoteConnectionInfo> _clients;
    private readonly ConcurrentDictionary<IPEndPoint, Guid> _endpoints;

    private readonly TcpListener _tcpListener;
    private readonly CancellationTokenSource _cancellationSource;
    private readonly IConnectionListener _connectionListener;
    private readonly ILogger<ServerConnectionAcceptor> _logger;
    private readonly Action<TcpClient> _clientSetup;

    private readonly Func<RemoteConnectionInfo, ValueTask> _disconnectFunc;
    private readonly Func<RemoteConnectionInfo, Exception, ValueTask> _disconnectErrorFunc;

    private bool _disposed;
    private TaskCompletionSource<bool>? _workingTask;

    //private readonly Socket _socketListener;

    public Task<bool> WorkingTask
    {
        get
        {
            _workingTask ??= new TaskCompletionSource<bool>();
            return _workingTask.Task;
        }
    }
    
    public ServerConnectionAcceptor(IPEndPoint endpoint, IConnectionListener listener, 
        ILogger<ServerConnectionAcceptor> logger, Action<TcpClient> clientSetup)
    {
        _clients = new();
        _endpoints = new();

        _tcpListener = new(endpoint);
        _cancellationSource = new();
        _connectionListener = listener;
        _logger = logger;
        _clientSetup = clientSetup;

        _disconnectFunc = DisconnectAsync;
        _disconnectErrorFunc = DisconnectWithErrorAsync;

        //_socketListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        Task.Run(ListenClientConnectionsAsync);
        Task.Run(CheckDisconnectsAsync);
    }

    private async Task CheckDisconnectsAsync()
    {
        var token = _cancellationSource.Token;

        for (; ; )
        {
            await Task.Delay(5000, token);

            foreach (var info in IPGlobal.GetConnectionsInfo())
            {
                if (info.State == ConnectionState.Connecting || info.State == ConnectionState.Connected)
                {
                    continue;
                }

                if (_endpoints.TryGetValue(info.RemoteEndPoint, out var clientUid))
                {
                    if (_clients.TryGetValue(clientUid, out var client))
                    {
                        if (client.TrySetClosingStatus())
                        {
                            _logger.LogInformation("{Uid} client starting disconnecting");
                            await client.DisconnectAsync();
                        }
                        else
                        {
                            _logger.LogInformation("Client {Uid} was not disconnected due " +
                                "disconnecting already started", clientUid);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Can't find client {Uid} for endpoint {Remote}", 
                            clientUid, info.RemoteEndPoint);
                    }
                }
                else
                {
                    //_logger.LogWarning("Can't find client for endpoint {EndPoint}", info.RemoteEndPoint);
                }
            }
        }
    }

    private async Task ListenClientConnectionsAsync()
    {
        var token = _cancellationSource.Token;

        try
        {
            _tcpListener.Start();
            _logger.LogInformation("Starting accepting connections");
        }
        catch (SocketException se)
        {
            _logger.LogCritical(se, "Accepting connections work failed");
            return;
        }

        try
        {
            for (; ;)
            {
                token.ThrowIfCancellationRequested();
                TcpClient tcpClient;

                try
                {
                    tcpClient = await _tcpListener.AcceptTcpClientAsync();
                    _clientSetup.Invoke(tcpClient);
                }
                catch (SocketException se)
                {
                    if (se.SocketErrorCode == SocketError.OperationAborted)
                    {
                        _logger.LogInformation(se, "Accepting connections stopped due listener closed");
                    }
                    else
                    {
                        _logger.LogCritical(se, "Accepting connections stopped due socket error");
                    }

                    throw;
                }

                var clientInfo = new RemoteConnectionInfo(Guid.NewGuid(), 
                    tcpClient, _disconnectFunc, _disconnectErrorFunc);

                try
                {
                    if (!await _connectionListener.OnConnectionVerifyAsync(clientInfo))
                    {
                        _logger.LogWarning("Uid: {Uid}; Connection verification failure", clientInfo.Uid);
                        tcpClient.Close();
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Uid: {Uid}; Connection vertification failure " +
                        "due ConnectionListener.OnConnectionVerifyAsync threw exception", clientInfo.Uid);

                    tcpClient.Close();
                    continue;
                }

                if (!_clients.TryAdd(clientInfo.Uid, clientInfo))
                {
                    throw new InternalDictionaryInconsistentException(nameof(_clients));
                }

                try
                {
                    await _connectionListener.OnConnectedAsync(clientInfo);
                    _logger.LogInformation("Uid: {Uid}; Client connected", clientInfo.Uid);

                    var client = (IPEndPoint)clientInfo.Client.Client.RemoteEndPoint;
                    _endpoints.TryAdd(client, clientInfo.Uid);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Uid: {Uid}; Client connection failure " +
                        "due ConnectionListener.OnConnectedAsync threw exception.", clientInfo.Uid);

                    tcpClient.Close();

                    if (!_clients.TryRemove(clientInfo.Uid, out _))
                    {
                        throw new InternalDictionaryInconsistentException(nameof(_clients));
                    }
                }
            }

            _workingTask?.SetResult(true);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Listening stopped due it was canceled");
            _workingTask?.SetResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Listening stopped");
            _workingTask?.SetResult(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _cancellationSource.Cancel();

        foreach (var clientInfo in _clients)
        {
            await DisconnectAsync(clientInfo.Value);
        }

        try
        {
            _tcpListener.Stop();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TcpListener threw exception when stopping");
        }
    }
    
    internal ValueTask DisconnectAsync(RemoteConnectionInfo clientInfo)
    {
        return clientInfo.TrySetClosingStatus()
            ? new ValueTask(DisconnectInternalAsync(clientInfo, null))
            : new ValueTask();
    }

    internal ValueTask DisconnectWithErrorAsync(RemoteConnectionInfo clientInfo, Exception exception)
    {
        return clientInfo.TrySetClosingStatus()
            ? new ValueTask(DisconnectInternalAsync(clientInfo, exception))
            : new ValueTask();
    }

    private async Task DisconnectInternalAsync(RemoteConnectionInfo clientInfo, Exception? exception)
    {
        if (!_clients.TryRemove(clientInfo.Uid, out _))
        {
            _logger.LogError("Uid: {Uid}; Client not found in internal collection while disconnecting", clientInfo.Uid);
        }

        try
        {
            await _connectionListener.OnDisconnectedAsync(clientInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uid: {Uid}; ConnectionListener threw exception while client disconnecting", clientInfo.Uid);
        }

        if (exception != null)
        {
            _logger.LogError(exception, "Uid: {Uid}; Client disconnected due error", clientInfo.Uid);

            if (exception.InnerException != null)
            {
                _logger.LogError(exception.InnerException, "Uid: {Uid}; Inner exception", clientInfo.Uid);
            }
        }

        clientInfo.Client.Dispose();
        _logger.LogInformation("Uid: {Uid}; Client disconnected", clientInfo.Uid);
    }

    private void CheckDisposed()
    {
        if (_disposed) ThrowDisposed();
    }

    private void ThrowDisposed()
    {
        throw new ObjectDisposedException(nameof(ServerConnectionAcceptor));
    }
}
