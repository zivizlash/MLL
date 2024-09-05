namespace MLL.Common.Layer;

public interface IHasExecuteDelegate
{
    Action<object?> ExecuteDelegate { get; }
}
