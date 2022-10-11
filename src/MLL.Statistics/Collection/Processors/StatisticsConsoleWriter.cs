using System.Text;

namespace MLL.Statistics.Collection.Processors;

public class StatisticsConsoleWriter : IStatProcessor
{
    private float _previousError;
    private float _previousRec1;
    private float _previousRec2;

    private readonly StringBuilder _st = new(1024);

    public void Process(StatisticsInfo stats)
    {
        var errorAcc = stats.ErrorStats.Errors.Sum(Math.Abs);
        var delta = errorAcc - _previousError;

        _st.Append($"Epoch {stats.EpochRange.Start:D4}-{stats.EpochRange.End:D4}; ");
        _st.AppendLine($"Error: {errorAcc:F5}; Delta: {delta:F5}; ");

        _previousRec1 = Write(_st, stats.TestStats, _previousRec1, true);
        _previousRec2 = Write(_st, stats.TrainStats, _previousRec2, false);

        _st.AppendLine();
        Console.WriteLine(_st.ToString());

        _previousError = errorAcc;
        _st.Clear();
    }

    private static float Write(StringBuilder st, NeuronRecognizedStats stats, float previous, bool isTestSet)
    {
        st.Append(isTestSet ? "Test  | " : "Train | ");
        st.Append($"Overall recognized percents: {stats.Total};");
        st.AppendLine($" Delta: {stats.Total - previous};");
        
        for (int i = 0; i < 10; i++)
        {
            st.Append($"{i}: {stats.Recognized[i]:F3}; ");
            if (i == 4) st.AppendLine();
        }

        st.AppendLine();
        return stats.Total;
    }

    public void Flush()
    {
    }
}
