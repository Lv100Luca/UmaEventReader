// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using UmaEventReader.Extensions;
using UmaEventReader.Model;

// await DbInitializer.Initialize(true);

await using var db = new UmaContext();

var eventName = "bottomless pit";

Console.Out.WriteLine("Searching event name: " + eventName);

var sw = new Stopwatch();
sw.Start();

var events = db.Events
    .Where(e => EF.Functions.ILike(e.EventName, $"%{eventName}%"))
    .Include(e => e.Choices)
    .ThenInclude(c => c.Outcomes)
    .ToList();

var searchTime = sw.ElapsedMilliseconds;

foreach (var umaEvent in events)
{
    Console.WriteLine($"Event ID: {umaEvent.Id}, Event Name: {umaEvent.EventName},  Character ID: {umaEvent.CharacterName}");

    foreach (var choice in umaEvent.Choices)
    {
        Console.WriteLine($"  Choice #{choice.ChoiceNumber}: {choice.ChoiceText} ({choice.SuccessType})");

        if (choice.Outcomes.Count > 0)
            Console.WriteLine("    Outcomes: " + string.Join(", ", choice.Outcomes));
    }
}

var printTime = sw.ElapsedMilliseconds;

Console.Out.WriteLine("Search: " + searchTime + "ms");
Console.Out.WriteLine("Print: " + printTime + "ms");