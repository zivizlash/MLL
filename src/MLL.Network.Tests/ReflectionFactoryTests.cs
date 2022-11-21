using MLL.Network.Factories;
using MLL.Network.Message.Protocol;
using Moq;
using NUnit.Framework;
using System;

namespace MLL.Network.Tests;

[TestFixture]
public class ReflectionFactoryTests
{
    public class EmptyCtorModel
    {
    }

    public class SenderCtorModel
    {
        public IMessageSender Sender { get; }

        public SenderCtorModel(IMessageSender sender)
        {
            Sender = sender;
        }
    }

    public class PreferSenderCtorModel
    {
        public IMessageSender? Sender { get; }

        public PreferSenderCtorModel(IMessageSender sender)
        {
            Sender = sender;
        }

        public PreferSenderCtorModel() { }
    }

    [Test]
    public void EmptyCtor()
    {
        var reflectionFactory = new ReflectionMessageHandlerFactory<EmptyCtorModel>();
        var handler = reflectionFactory.CreateMessageHandler(new MessageHandlerFactoryContext());
        Assert.IsInstanceOf<EmptyCtorModel>(handler);
    }

    [Test]
    public void SenderCtor()
    {
        var senderMock = Mock.Of<IMessageSender>();
        var reflectionFactory = new ReflectionMessageHandlerFactory<SenderCtorModel>();
        var handler = reflectionFactory.CreateMessageHandler(new(senderMock, Guid.NewGuid()));

        Assert.IsInstanceOf<SenderCtorModel>(handler);
        Assert.IsNotNull(((SenderCtorModel) handler).Sender);
    }

    [Test]
    public void PreferSenderCtor()
    {
        var senderMock = Mock.Of<IMessageSender>();
        var reflectionFactory = new ReflectionMessageHandlerFactory<PreferSenderCtorModel>();
        var handler = reflectionFactory.CreateMessageHandler(new(senderMock, Guid.NewGuid()));

        Assert.IsInstanceOf<PreferSenderCtorModel>(handler);
        Assert.IsNotNull(((PreferSenderCtorModel)handler).Sender);
    }
}
