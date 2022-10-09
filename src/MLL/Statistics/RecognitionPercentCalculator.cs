using MLL.Common.Files;
using MLL.Common.Net;
using MLL.Common.Tools;
using MLL.Files.ImageLoader;

namespace MLL.Statistics;

public struct RecognitionPercentCalculator
{
    public static void Calculate(NetManager net, IImageDataSetProvider dataSetProvider, Span<float> percentage)
    {
        Check.LengthEqual(percentage.Length, 10, nameof(percentage));

        for (int imageNumber = 0; imageNumber < 10; imageNumber++)
        {
            var dataSet = dataSetProvider.GetDataSet(imageNumber);
            
            for (int imageIndex = 0; imageIndex < dataSet.Count; imageIndex++)
            {
                var result = net.Predict(dataSet[imageIndex].Data);

                if (FindBiggestIndex(result) == imageNumber)
                    percentage[imageNumber] += 1;
            }

            percentage[imageNumber] /= dataSet.Count;
        }
    }
    
    private static int FindBiggestIndex(ReadOnlySpan<float> values)
    {
        int index = 0;
        float value = values[0];

        for (int i = 1; i < values.Length; i++)
        {
            float comparand = values[i];

            if (comparand > value)
            {
                value = comparand;
                index = i;
            }
        }

        return index;
    }
}
