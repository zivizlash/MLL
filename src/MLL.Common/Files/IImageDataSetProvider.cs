namespace MLL.Common.Files;

public interface IImageDataSetProvider
{
    IImageDataSet GetDataSet(string name, object value);
    int GetLargestImageDataSetCount(IEnumerable<(string name, object value)> indices);
    void LoadAllImages(IEnumerable<(string name, object value)> indices);
}
