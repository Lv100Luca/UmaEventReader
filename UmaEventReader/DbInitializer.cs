using System.Text.Json;
using UmaEventReader.Model;
using UmaEventReader.Model.dto;

namespace UmaEventReader;

public class DbInitializer
{
    private const string JsonFile = "umaDb.json";

    public async static Task Initialize(bool clearDb = false)
    {
        await using var db = new UmaContext();

        if (clearDb)
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }

        var json = await File.ReadAllTextAsync(JsonFile);
        var root = JsonSerializer.Deserialize<Root>(json);

        if (root == null)
            return;

        // Group by EventName
        var grouped = root.ChoiceArraySchema.EventChoices
            .GroupBy(c => c.EventName);

        foreach (var group in grouped)
        {
            var umaEvent = Mapper.ToUmaEvent(group.ToList());
            db.Events.Add(umaEvent);
        }

        await db.SaveChangesAsync();
    }
}