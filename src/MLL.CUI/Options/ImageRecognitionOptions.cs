namespace MLL.CUI.Options;

public readonly struct ImageRecognitionOptions
{
    public int ImageWidth { get; }
    public int ImageHeight { get; }
    public double Bias { get; }
    public float LearningRate { get; }
    public int? RandomSeed { get; }

    public ImageRecognitionOptions(int imageWidth, int imageHeight, 
        double bias, float learningRate, int? randomSeed)
    {
        ImageWidth = imageWidth;
        ImageHeight = imageHeight;
        Bias = bias;
        LearningRate = learningRate;
        RandomSeed = randomSeed;
    }

    public static readonly ImageRecognitionOptions Default = new(32, 32, 10, 0.00005f, 234578); 
}
