// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UmaEventReader;
using UmaEventReader.Model;
using UmaEventReader.Model.dto;

// await DbInitializer.Initialize(true);

await using var db = new UmaContext();

var eventName = "bottomless pit";

Console.Out.WriteLine("Searching event name: " + eventName);

var events = db.Events
    .Where(e => EF.Functions.ILike(e.EventName, $"%{eventName}%"))
    .Include(e => e.Choices)
    .ThenInclude(c => c.Outcomes)
    .ToList();

foreach (var umaEvent in events)
{
    Console.WriteLine($"Event ID: {umaEvent.Id}, Event Name: {umaEvent.EventName},  Character ID: {umaEvent.CharacterName}");

    foreach (var choice in umaEvent.Choices)
    {
        Console.WriteLine($"  Choice #{choice.ChoiceNumber}: {choice.ChoiceText}");

        if (choice.Outcomes.Count > 0)
            Console.WriteLine("    Outcomes: " + string.Join(", ", choice.Outcomes.Select(o => o.Value)));
    }
}