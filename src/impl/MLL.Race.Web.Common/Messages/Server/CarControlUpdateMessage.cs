namespace MLL.Race.Web.Common.Messages.Server;

public class CarControlUpdateMessage
{
    public float Forward { get; }
    public float Left { get; }

    public CarControlUpdateMessage(float forward, float left)
    {
        Forward = forward;
        Left = left;
    }
}
