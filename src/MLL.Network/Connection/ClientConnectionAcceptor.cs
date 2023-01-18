using MLL.Network.Tools;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MLL.Network.Message.Protocol;

public class ClientConnectionAcceptor : IDisposable, IAsyncDisposable
{
    private readonly IPEndPoint _endpoint;
    private readonly IConnectionListener _listener;
    private readonly TcpClient _tcpClient;

    private volatile RemoteConnectionInfo? _clientInfo;
    private bool _disposed;

    public ClientConnectionAcceptor(IPEndPoint endpoint, IConnectionListener listener)
    {
        _endpoint = endpoint;
        _listener = listener;
        _tcpClient = new();
    }

    public async Task ConnectAsync()
    {
        CheckDisposed();

        await _tcpClient.ConnectAsync(_endpoint.Address, _endpoint.Port);

        _clientInfo = new RemoteConnectionInfo(Guid.NewGuid(), _tcpClient, 
            _ => new ValueTask(), (_, _) => new ValueTask());

        if (!await _listener.OnConnectionVerifyAsync(_clientInfo))
        {
            throw new InvalidOperationException("Connection verification failed.");
        }

        await _listener.OnConnectedAsync(_clientInfo);
    }

    private void CheckDisposed()
    {
        if (_disposed) Throw.Disposed(nameof(ClientConnectionAcceptor));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (_clientInfo != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await _listener.OnDisconnectedAsync(_clientInfo);
                    }
                    finally
                    {
                        _tcpClient.Close();
                    }
                });
            }
        }
        catch { }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_clientInfo != null)
        {
            try
            {
                await _listener.OnDisconnectedAsync(_clientInfo);
            }
            catch
            {
            }
            finally
            {
                _tcpClient.Dispose();
            }
        }
    }
}
