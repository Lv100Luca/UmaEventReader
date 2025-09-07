using UmaEventReader.Model.Enums;

namespace UmaEventReader.Model;

public class Outcome
{
    public long Id { get; set; }
    public OutcomeType Type { get; set; }
    public string Value { get; set; } = string.Empty;

    public long UmaEventChoiceId { get; set; }
    public UmaEventChoice UmaEventChoice { get; set; } = null!;
}