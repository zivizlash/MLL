namespace MLL.Common.Layer;

public interface IHasExecuteDelegate
{
    WaitCallback ExecuteDelegate { get; }
}
