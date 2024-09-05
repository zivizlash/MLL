using MLL.Common.Tools;

namespace MLL.Repository;

public class NetDatabase
{
    private readonly DirectoryInfo _netsDirectory;

    public NetDatabase(string folder)
    {
        _netsDirectory = Directory.CreateDirectory(folder).CreateSubdirectory("nets");
    }

    public INetInfo OpenOrCreate(NetCreation creation, Action<INetInfo> onCreatedAction)
    {
        var netPath = Path.Combine(_netsDirectory.FullName, ConvertNameToFolderName(creation.Name));
        var netInfo = new NetInfo(netPath);

        if (netInfo.IsJustCreated)
        {
            onCreatedAction.Invoke(netInfo);
        }

        return netInfo;
    }

    private static string ConvertNameToFolderName(string name)
    {
        var folderName = string.Join("", name
            .Select(ch => ch == ' ' ? '_' : ch)
            .Where(ch => char.IsLetterOrDigit(ch) || ch == '_'));

        if (string.IsNullOrEmpty(folderName))
        {
            Throw.Argument(nameof(NetCreation.Name), "Name must contains at least one letter, digit or space char");
        }

        return folderName;
    }
}
