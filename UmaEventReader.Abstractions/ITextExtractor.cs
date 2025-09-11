using System.Drawing;
using UmaEventReader.Abstractions.Model;

namespace UmaEventReader.Abstractions;

public interface ITextExtractor
{
    public Task<TextExtractorResult> ExtractTextAsync(Bitmap bmpImage);
}