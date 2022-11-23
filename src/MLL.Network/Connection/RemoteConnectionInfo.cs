using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MLL.Network.Message.Protocol;

public class RemoteConnectionInfo
{
    private readonly Func<RemoteConnectionInfo, ValueTask> _disconnect;

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

    internal RemoteConnectionInfo(Guid uid, TcpClient client, Func<RemoteConnectionInfo, ValueTask> disconnect)
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
