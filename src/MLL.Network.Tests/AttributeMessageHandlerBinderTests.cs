using MLL.Network.Message.Handlers;
using MLL.Network.Message.Handlers.Binding;
using NUnit.Framework;
using System.Threading.Tasks;

namespace MLL.Network.Tests;

[TestFixture]
public class AttributeMessageHandlerBinderTests
{
    public class TestHandler
    {
        public int IntValue { get; set; }
        public TestMessage? TestMessageValue { get; set; }

        [MessageHandler]
        public Task IntTestAsync(int value)
        {
            IntValue = value;
            return Task.CompletedTask;
        }

        [MessageHandler]
        public void TestMessageAsync(TestMessage testMessage)
        {
            TestMessageValue = testMessage;
        }
    }

    [Test]
    public async Task TestAsync()
    {
        var attributeBinder = new AttributeMessageHandlerBinder();

        var handler1 = new TestHandler();
        var handler2 = new TestHandler();

        var messageHandler = attributeBinder.Bind(typeof(TestHandler), handler1);

        await messageHandler.HandleAsync(127);
        await messageHandler.HandleAsync(new TestMessage { StringValue = "156", IntValue = 12 });

        Assert.AreEqual(127, handler1.IntValue);
        Assert.AreEqual("156", handler1.TestMessageValue?.StringValue);
        Assert.AreEqual(12, handler1.TestMessageValue?.IntValue);

        Assert.AreEqual(default(int), handler2.IntValue);
        Assert.AreEqual(null, handler2.TestMessageValue);
    }
}
