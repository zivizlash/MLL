using System;
using System.Net.Sockets;

namespace MLL.Network.Message.Protocol;

public struct TcpConnectionInfo
{
    public TcpClient Client;
    public TimeSpan RequestTimeout;

    public TcpConnectionInfo(TcpClient client)
    {
        RequestTimeout = TimeSpan.FromMinutes(1);
        Client = client;
    }
}
