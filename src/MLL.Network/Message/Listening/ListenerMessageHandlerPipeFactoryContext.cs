using MLL.Network.Message.Protocol;

namespace MLL.Network.Message.Listening;

public readonly struct ListenerMessageHandlerPipeFactoryContext
{
    public readonly ClientConnectionInfo ClientInfo;

    public ListenerMessageHandlerPipeFactoryContext(ClientConnectionInfo clientInfo)
    {
        ClientInfo = clientInfo;
    }
}
