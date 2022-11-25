using MLL.Network.Message.Converters;
using System.Threading;
using System.Threading.Tasks;

namespace MLL.Network.Message.Protocol;

public class MessageSender : IMessageSender
{
    private readonly MessageTcpProtocol _protocol;
    private readonly MessageConverter _converter;

    public MessageSender(MessageTcpProtocol protocol, MessageConverter converter)
    {
        _protocol = protocol;
        _converter = converter;
    }

    public async Task SendAsync<T>(T message, CancellationToken cancellationToken)
    {
        var (messageBytes, messageType) = _converter.Serialize(message);
        await _protocol
            .WriteAsync(messageBytes, messageType, cancellationToken)
            .ConfigureAwait(false);
    }
}
