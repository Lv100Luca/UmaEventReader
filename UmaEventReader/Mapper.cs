using UmaEventReader.Model;
using UmaEventReader.Model.dto;
using UmaEventReader.Model.Enums;

public static class Mapper
{
    public static UmaEvent ToUmaEvent(List<EventChoice> choices)
    {
        if (choices.Count == 0)
            throw new ArgumentException("No choices provided");

        var umaEvent = new UmaEvent
        {
            EventName = choices.First().EventName,
            CharacterName = choices.First().CharacterName,
            Choices = choices.Select(ToUmaEventChoice).ToList()
        };

        // assign FK
        foreach (var choice in umaEvent.Choices)
        {
            choice.UmaEvent = umaEvent;
        }

        return umaEvent;
    }

    private static UmaEventChoice ToUmaEventChoice(EventChoice dto)
    {
        return new UmaEventChoice
        {
            ChoiceNumber = int.Parse(dto.ChoiceNumber),
            ChoiceText = dto.ChoiceText,
            SuccessType = Enum.TryParse<SuccessType>(dto.SuccessType, out var s) ? s : SuccessType.None,
            Outcomes = ParseOutcomes(dto.AllOutcomes)
        };
    }

    private static List<Outcome> ParseOutcomes(string allOutcomes)
    {
        return allOutcomes
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => new Outcome { Value = part.Trim(), Type = OutcomeType.Unknown })
            .ToList();
    }
}