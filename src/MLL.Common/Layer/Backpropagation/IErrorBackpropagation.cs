namespace MLL.Common.Layer.Backpropagation;

public interface IErrorBackpropagation
{
    void ReorganizeErrors(BackpropContext ctx, float[] errors);
}
