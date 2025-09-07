namespace UmaEventReader.Model;

public class UmaEvent
{
    public long Id { get; set; }
    public string CharacterName { get; set; }
    public string EventName { get; set; }

    public List<UmaEventChoice> Choices { get; set; } = [];
}