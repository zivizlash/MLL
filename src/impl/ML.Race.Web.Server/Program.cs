using ML.Race.Web.Server.Handler;
using MLL.Network.Builders;
using MLL.Network.Factories;
using MLL.Race.Web.Common.Messages;
using System.Net;

public class Program
{
    public static async Task Main(string[] args)
    {
        var typesProvider = new MessageTypesProvider();

        foreach (var type in typesProvider.GetTypes())
        {
            Console.WriteLine(type.FullName);
        }

        using var server = new ConnectionManagerBuilder()
            .WithAddress(new IPEndPoint(IPAddress.Any, 8888))
            .WithHandlerFactory(new ReflectionHandlerFactory<ServerMessageHandler>())
            .WithUsedTypes(typesProvider)
            .BuildServer();

        await server.WorkingTask;
    }
}
