using System.Drawing;
using Tesseract;
using UmaEventReader.Abstractions;
using UmaEventReader.Abstractions.Model;

namespace UmaEventReader.Services;

public class TesseractTextExtractor : ITextExtractor
{
    private const string TesseractTraineeDataPath = "tessdata";
    private const string CharWhitelist = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,!?'()#☆ ";
    private const PageSegMode DefaultPageSegMode = PageSegMode.SingleLine;

    private TesseractEngine Engine { get; init; }

    public TesseractTextExtractor()
    {
        Engine = InitEngine();
        ConfigureEngine();
    }

    public Task<TextExtractorResult> ExtractTextAsync(Bitmap bmpImage)
    {
        var pix = PixConverter.ToPix(bmpImage);

        using var page = Engine.Process(pix);

        var result = new TextExtractorResult
        {
            Text = page.GetText(),
            Metadata = new TextExtractorResultMetadata
            {
                MeanConfidence = page.GetMeanConfidence()
            }
        };
        
        return Task.FromResult(result);
    }

    private static TesseractEngine InitEngine()
    {
        return new TesseractEngine(TesseractTraineeDataPath, "eng", EngineMode.Default);
    }

    private void ConfigureEngine()
    {
        Engine.SetVariable("tessedit_char_whitelist", CharWhitelist);
        Engine.DefaultPageSegMode = DefaultPageSegMode;
    }
}