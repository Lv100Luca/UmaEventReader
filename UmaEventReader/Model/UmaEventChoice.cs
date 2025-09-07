using UmaEventReader.Model.Enums;

namespace UmaEventReader.Model;

public class UmaEventChoice
{
    public long Id { get; set; }
    public int ChoiceNumber { get; set; }
    public string ChoiceText { get; set; } = string.Empty;
    public SuccessType SuccessType { get; set; }

    public long UmaEventId { get; set; }   // FK
    public UmaEvent UmaEvent { get; set; } = null!;

    public List<Outcome> Outcomes { get; set; } = new();
}