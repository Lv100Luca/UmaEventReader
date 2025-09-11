namespace UmaEventReader.Abstractions.Model;

public class TextExtractorResult
{
    public required string Text { get; init; }
    public TextExtractorResultMetadata Metadata { get; init; } = new();
}