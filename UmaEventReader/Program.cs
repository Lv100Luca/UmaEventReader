// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UmaEventReader;
using UmaEventReader.Model;
using UmaEventReader.Model.dto;

await DbInitializer.Initialize(true);

// using var db = new UmaContext();

// var jsonFile = "umaDb.json";
// db.Database.EnsureCreated(); // or use Migrations
//
// var json = File.ReadAllText(jsonFile);
// var root = JsonSerializer.Deserialize<Root>(json);
//
// if (root?.ChoiceArraySchema.EventChoices is not null)
// {
//     // Group by EventName
//     var grouped = root.ChoiceArraySchema.EventChoices
//         .GroupBy(c => c.EventName);
//
//     foreach (var group in grouped)
//     {
//         var umaEvent = Mapper.ToUmaEvent(group.ToList());
//         db.Events.Add(umaEvent);
//     }
//
//     db.SaveChanges();
// }

// var events = db.Events
//     .Where(e => e.EventName.Contains("Best Foot"))
//     .Include(e => e.Choices)
//     .ThenInclude(c => c.Outcomes)
//     .ToList();
//
// foreach (var umaEvent in events)
// {
//     Console.WriteLine($"Event ID: {umaEvent.Id}, Event Name: {umaEvent.EventName},  Character ID: {umaEvent.CharacterName}");
//
//     foreach (var choice in umaEvent.Choices)
//     {
//         Console.WriteLine($"  Choice #{choice.ChoiceNumber}: {choice.ChoiceText}");
//
//         if (choice.Outcomes.Count > 0)
//             Console.WriteLine("    Outcomes: " + string.Join(", ", choice.Outcomes.Select(o => o.Value)));
//     }
// }