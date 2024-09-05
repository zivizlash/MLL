using Microsoft.Extensions.Logging.Abstractions;
using MLL.Network.Builders;
using MLL.Network.Factories;
using MLL.Network.Message.Handlers;
using MLL.Network.Message.Protocol;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MLL.Network.Tests;

public class ServerTestMessageHandler
{
    public readonly IMessageSender MessageSender;

    public PingMessage? LastPingMessage { get; private set; }

    public GameFrameMessage? GameFrame;

    public ServerTestMessageHandler(IMessageSender messageSender)
    {
        MessageSender = messageSender;
    }

    [MessageHandler]
    public async ValueTask PingHandler(PingMessage ping)
    {
        LastPingMessage = ping;
        if (ping.Count == 0) return;

        await MessageSender.SendAsync(new PongMessage 
        { 
            PongValue = ping.PingValue,
            PongSquareValue = ping.PingValue * ping.PingValue,
            Count = ping.Count - 1
        });
    }

    [MessageHandler]
    public void GameFrameHandler(GameFrameMessage gameFrame)
    {
        GameFrame = gameFrame;
    }
}

public class ClientTestMessageHandler
{
    public readonly IMessageSender MessageSender;

    public volatile GameFrameMessage? GameFrame;

    private volatile int _pongCallsCount;

    public int PongCallsCount 
    {
        get => _pongCallsCount; private set => _pongCallsCount = value;
    }

    public ClientTestMessageHandler(IMessageSender sender)
    {
        MessageSender = sender;
    }

    public async Task SendPing(int pingValue, int count)
    {
        await MessageSender.SendAsync(new PingMessage
        {
            PingValue = pingValue,
            Count = count
        });
    }

    [MessageHandler]
    public async Task PongHandler(PongMessage pong)
    {
        PongCallsCount++;

        await MessageSender.SendAsync(new PingMessage
        {
            PingValue = pong.PongValue,
            Count = pong.Count
        });
    }
}

[TestFixture]
public class IntegrationProtocolTest
{
    public IntegrationProtocolTest()
    {
    }

    [Test]
    public async Task ServerManagerTest()
    {
        var acceptableTypes = new Type[] 
        { 
            typeof(PingMessage), typeof(PongMessage), typeof(GameFrameMessage) 
        };

        var serverFactory = new ReflectionHandlerFactory<ServerTestMessageHandler>();
        var serverSingleton = new SingletonHandlerFactory<ServerTestMessageHandler>(serverFactory);

        await using var serverManager = new ConnectionManagerBuilder()
            .WithAddress(new IPEndPoint(IPAddress.Any, 8888))
            .WithHandlerFactory(serverSingleton)
            .WithUsedTypes(acceptableTypes)
            .WithLoggerFactory(new NullLoggerFactory())
            .BuildServer();

        var clientEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);

        var cancellationSource = new CancellationTokenSource(TimeSpan.FromMinutes(15));
        var token = cancellationSource.Token;

        var clientFactory = new ReflectionHandlerFactory<ClientTestMessageHandler>();
        var clientSingleton = new SingletonHandlerFactory<ClientTestMessageHandler>(clientFactory);

        using var clientManager = new ConnectionManagerBuilder()
            .WithAddress(clientEndpoint)
            .WithHandlerFactory(clientSingleton)
            .WithUsedTypes(acceptableTypes)
            .WithLoggerFactory(new NullLoggerFactory())
            .BuildClient();

        const int pingValue = 100;
        const int pingRepeats = 5;

        await clientManager.ConnectAsync();
        await clientSingleton.Instance.SendPing(pingValue, pingRepeats);

        await clientSingleton.Instance.MessageSender.SendAsync(new GameFrameMessage
        {
            Frame = new byte[] { 1, 2, 3, 4, 5 },
            ElapsedTime = 0
        });

        var spin = new SpinWait();

        while (serverSingleton.Instance.GameFrame == null)
        {
            token.ThrowIfCancellationRequested();
            spin.SpinOnce();
        }

        Assert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, serverSingleton.Instance.GameFrame!.Frame);

        while (clientSingleton.Instance.PongCallsCount != pingRepeats)
        {
            token.ThrowIfCancellationRequested();
            spin.SpinOnce();
        }

        Assert.AreEqual(pingValue, serverSingleton.Instance.LastPingMessage!.PingValue);
    }
}
