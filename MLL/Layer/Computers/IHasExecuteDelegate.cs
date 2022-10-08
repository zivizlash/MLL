namespace MLL.Layer.Computers;

public interface IHasExecuteDelegate
{
    WaitCallback ExecuteDelegate { get; }
}
