using System.Threading.Tasks;

namespace MLL.Network.Message.Protocol;

public interface IConnectionListener
{
    ValueTask OnConnectedAsync(RemoteConnectionInfo clientInfo);
    ValueTask<bool> OnConnectionVerifyAsync(RemoteConnectionInfo clientInfo);
    ValueTask OnDisconnectedAsync(RemoteConnectionInfo clientInfo);
}
