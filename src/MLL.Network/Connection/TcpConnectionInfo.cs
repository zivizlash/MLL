using System;
using System.Net.Sockets;

namespace MLL.Network.Message.Protocol;

public readonly struct TcpConnectionInfo
{
    public readonly TcpClient Client;
    public readonly TimeSpan RequestTimeout;
    public readonly Guid Uid;

    public TcpConnectionInfo(TcpClient client, Guid uid)
    {
        RequestTimeout = TimeSpan.FromMinutes(1);
        Client = client;
        Uid = uid;
    }
}
