// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using UmaEventReader.Extensions;
using UmaEventReader.Model;
using UmaEventReader.Services;

await DbInitializer.Initialize(true);
await using var db = new UmaContext();

_ = SearchEvents("pit"); // workaround for first search being slow

var done = false;

while (!done)
{
    Console.Out.Write("Event name: ");
    var eventName = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(eventName))
    {
        done = true;
        continue;
    }

    RunSearch(eventName);
}


return;

List<UmaEvent> SearchEvents(string eventName)
{
    return db.Events
        .AsNoTracking() // loads the db entries as 'read only' -> changes wont be reflected in the db
        .WhereEventNameContains(eventName)
        .Include(e => e.Choices)
        .ThenInclude(c => c.Outcomes)
        .ToList();
}

void RunSearch(string eventName)
{
    Console.Out.WriteLine("Searching event name: " + eventName);

    var sw = new Stopwatch();
    sw.Start();

    var events = SearchEvents(eventName);


    var searchTime = sw.ElapsedMilliseconds;

    foreach (var umaEvent in events)
    {
        Console.Out.WriteLine(umaEvent);

        Console.Out.WriteLine("======================================");
    }

    Console.Out.WriteLine("Found " + events.Count + $" events in {searchTime}ms");
}