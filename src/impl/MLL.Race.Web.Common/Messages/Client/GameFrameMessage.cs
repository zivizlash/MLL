namespace MLL.Race.Web.Common.Messages.Client;

public class GameFrameMessage
{
    public byte[] Frame { get; }
    public float ElapsedTime { get; }

    public GameFrameMessage(byte[] frame, float elapsedTime)
    {
        Frame = frame;
        ElapsedTime = elapsedTime;
    }
}
