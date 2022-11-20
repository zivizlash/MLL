using MLL.Network.Message.Protocol;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MLL.Network.Message.Listening;

public class RawMessageListeningPipe
{
    private readonly MessageTcpProtocol _messageProtocol;
    private readonly Channel<RawMessage> _channel;

    private CancellationTokenSource? _tokenSource;

    public ChannelReader<RawMessage> Reader => _channel.Reader;

    public RawMessageListeningPipe(MessageTcpProtocol messageProtocol)
    {
        _messageProtocol = messageProtocol;
        _channel = Channel.CreateBounded<RawMessage>(new BoundedChannelOptions(10)
        {
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public void StartOrRestart()
    {
        _tokenSource?.Cancel();
        _tokenSource = new CancellationTokenSource();

        var token = _tokenSource.Token;
        var writer = _channel.Writer;

        Task.Factory.StartNew(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                var message = await _messageProtocol.ReadAsync(token);
                await writer.WriteAsync(message, token);
            }
        });
    }

    public void Stop()
    {
        _tokenSource?.Cancel();
    }
}
