using MLL.Network.Message.Protocol;

namespace MLL.Network.Message.Listening;

public readonly struct ListenerMessageHandlerPipeFactoryContext
{
    public readonly RemoteConnectionInfo ClientInfo;

    public ListenerMessageHandlerPipeFactoryContext(RemoteConnectionInfo clientInfo)
    {
        ClientInfo = clientInfo;
    }
}
