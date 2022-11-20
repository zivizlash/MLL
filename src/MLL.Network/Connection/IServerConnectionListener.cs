using System.Threading.Tasks;

namespace MLL.Network.Message.Protocol;

public interface IServerConnectionListener
{
    ValueTask OnConnectedAsync(ClientConnectionInfo clientInfo);
    ValueTask<bool> OnConnectionVerifyAsync(ClientConnectionInfo clientInfo);
    ValueTask OnDisconnectedAsync(ClientConnectionInfo clientInfo);
}
