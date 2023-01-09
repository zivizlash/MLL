namespace MLL.Race.Web.Server;

public class AdaptiveLearningRate
{
    private readonly int _newThreshold;
    private readonly int _oldThreshold;
    private readonly float _minimum;

    public const float IncreaseMult = 1.5f;
    public const float DecreaseMult = 0.95f;

    private int _newSelectCount = 0;
    private int _oldSelectCount = 0;

    public float LearningRate { get; private set; }

    public AdaptiveLearningRate(float initialLearningRate, 
        int newThreshold, int oldThreshold, float minimum)
    {
        LearningRate = initialLearningRate;
        _newThreshold = newThreshold;
        _oldThreshold = oldThreshold;
        _minimum = minimum;
    }

    public float SelectOldAndGet()
    {
        _newSelectCount = 0;

        if (++_oldSelectCount == _oldThreshold)
        {
            _oldSelectCount = 0;
            LearningRate = Math.Max(_minimum, LearningRate * DecreaseMult);
        }

        return LearningRate;
    }

    public float SelectNewAndGet()
    {
        _oldSelectCount = 0;

        if (++_newSelectCount == _newThreshold)
        {
            _newSelectCount = 0;
            LearningRate *= IncreaseMult;
        }

        return LearningRate;
    }
}
