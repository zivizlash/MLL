
using MLL.Race.Web.Common.Messages;

public class Program
{
    public static void Main(string[] args)
    {
        foreach (var type in new MessageTypesProvider().GetTypes())
        {
            Console.WriteLine(type.FullName);
        }
    }
}
