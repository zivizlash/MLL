namespace MLL;

public readonly struct ImageRecognitionOptions
{
    public int ImageWidth { get; }
    public int ImageHeight { get; }
    public double Bias { get; }
    public double LearningRate { get; }
    public int? RandomSeed { get; }

    public ImageRecognitionOptions(int imageWidth, int imageHeight, double bias, 
        double learningRate, int? randomSeed)
    {
        ImageWidth = imageWidth;
        ImageHeight = imageHeight;
        Bias = bias;
        LearningRate = learningRate;
        RandomSeed = randomSeed;
    }

    public static readonly ImageRecognitionOptions Default = new(128, 128, 10, 0.0005, 2357678);
}
