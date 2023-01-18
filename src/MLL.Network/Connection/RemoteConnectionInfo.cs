using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MLL.Network.Message.Protocol;

public class RemoteConnectionInfo
{
    private readonly Func<RemoteConnectionInfo, ValueTask> _disconnect;
    private readonly Func<RemoteConnectionInfo, Exception, ValueTask> _expDisconnect;

    private volatile int _status;

#pragma warning disable IDE1006 // Naming Styles
    private const int StatusWorking = 0;
    private const int StatusClosing = 1;
#pragma warning restore IDE1006 // Naming Styles

    public Guid Uid { get; set; }
    public TcpClient Client { get; }
    public Socket Socket { get; set; }

    internal bool IsClosing => _status == StatusClosing;

    internal bool TrySetClosingStatus()
    {
        var original = Interlocked.CompareExchange(ref _status, StatusClosing, StatusWorking);
        return original == StatusWorking;
    }

    internal RemoteConnectionInfo(Guid uid, Socket socket, 
        Func<RemoteConnectionInfo, ValueTask> disconnect,
        Func<RemoteConnectionInfo, Exception, ValueTask> expDisconnect)
    {
        Uid = uid;
        Socket = socket;
        _disconnect = disconnect;
        _expDisconnect = expDisconnect;
    }

    internal ValueTask DisconnectWithErrorAsync(Exception ex)
    {
        if (TrySetClosingStatus())
        {
            return _expDisconnect.Invoke(this, ex);
        }

        return new ValueTask();
    }

    public ValueTask DisconnectAsync()
    {
        if (TrySetClosingStatus())
        {
            return _disconnect.Invoke(this);
        }

        return new ValueTask();
    }
}
