namespace MLL.Common.Scoring;

public interface IErrorToScoreConverter
{
    float ErrorToScore(float error);
}

public class DefaultErrorToScoreConverter : IErrorToScoreConverter
{
    public float ErrorToScore(float error)
    {
        throw new NotImplementedException();
    }
}
