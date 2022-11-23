using MessagePack;
using MessagePack.Resolvers;
using MLL.Network.Builders;
using MLL.Network.Factories;
using MLL.Network.Message.Converters;
using MLL.Network.Message.Handlers;
using MLL.Network.Message.Listening;
using MLL.Network.Message.Protocol;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MLL.Network.Tests;

public class TestMessageHandler
{
    private readonly IMessageSender _messageSender;

    public TestMessageHandler(IMessageSender messageSender)
    {
        _messageSender = messageSender;
    }

    [MessageHandler]
    public async ValueTask PingHandler(PingMessage pingMessage)
    {
        await _messageSender.SendAsync(new PongMessage 
        { 
            PongValue = pingMessage.PingValue,
            PongSquareValue = pingMessage.PingValue * pingMessage.PingValue
        });
    }
}

[TestFixture]
public class IntegrationProtocolTest
{
    private readonly MessagePackSerializerOptions _options;

    public IntegrationProtocolTest()
    {
        _options = MessagePackSerializer.DefaultOptions
            .WithResolver(ContractlessStandardResolver.Instance);
    }

    [Test]
    public async Task ServerManagerTest()
    {
        var acceptableTypes = new List<Type> { typeof(PingMessage), typeof(PongMessage) };

        using var serverManager = new ConnectionManagerBuilder()
            .WithAddress(new IPEndPoint(IPAddress.Any, 8888))
            .WithHandlerFactory(new ReflectionMessageHandlerFactory<TestMessageHandler>())
            .WithUsedTypes(acceptableTypes.ToArray())
            .BuildServer();

        var hashCode = new ProtocolVersionHashCode();
        var messageConverter = new MessageConverter(acceptableTypes, hashCode);

        var clientEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);

        var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var token = cancellationSource.Token;

        using var client = new TcpClient();
        
        await client.ConnectAsync(clientEndpoint, token);
        var clientProtocol = new MessageTcpProtocol(new TcpConnectionInfo(client));

        var testMessage = new PingMessage { PingValue = 10 };

        var (messageBytes, messageType) = messageConverter.Serialize(testMessage);
        await clientProtocol.WriteAsync(messageBytes, messageType, token);

        var message = await clientProtocol.ReadAsync(token);
        var result = MessagePackSerializer.Deserialize<PongMessage>(message.Data, _options);

        Assert.AreEqual(10 * 10, result.PongSquareValue);
    }

    [Test]
    public async Task TcpTest()
    {
        var cancellationSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var token = cancellationSource.Token;
        var address = new IPEndPoint(IPAddress.Any, 8888);
        var address2 = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);

        RawMessage serverReceivedMessage = default;

        using var messageEvent = new ManualResetEventSlim(false);
        using var serverStartEvent = new ManualResetEventSlim(false);

        var serverTask = Task.Factory.StartNew(async () =>
        {
            var server = new TcpListener(address);
            RawMessageListeningPipe? listeningPipe = null;
            
            try
            {
                server.Start();
                serverStartEvent.Set();

                var serverClient = await server.AcceptTcpClientAsync(token);
                var connection = new TcpConnectionInfo(serverClient);

                var protocol = new MessageTcpProtocol(connection);

                listeningPipe = new RawMessageListeningPipe(protocol);
                listeningPipe.StartOrRestart();

                serverReceivedMessage = await listeningPipe.Reader.ReadAsync(token);
                messageEvent.Set();
            }
            finally
            {
                listeningPipe?.Stop();
                messageEvent.Set();
                serverStartEvent.Set();
                server.Stop();
            }
        }).Unwrap();

        using var client = new TcpClient();
        serverStartEvent.Wait(token);

        await client.ConnectAsync(address2, token);
        var clientProtocol = new MessageTcpProtocol(new TcpConnectionInfo(client));

        var testMessage = new TestMessage { IntValue = 10, StringValue = "Hello there" };
        await clientProtocol.WriteAsync(MessagePackSerializer.Serialize(testMessage, _options), 0, token);

        messageEvent.Wait(token);

        var result = MessagePackSerializer.Deserialize<TestMessage>(
            serverReceivedMessage.Data, _options);

        Assert.AreEqual(10, result.IntValue);
        Assert.AreEqual("Hello there", result.StringValue);
    }
}
