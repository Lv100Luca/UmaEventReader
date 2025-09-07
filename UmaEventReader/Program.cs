// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Tesseract;
using UmaEventReader;
using UmaEventReader.Extensions;
using UmaEventReader.Model;
using UmaEventReader.Services;
await using var db = new UmaContext();
await DbInitializer.Initialize(true);

// TextSearch();
SearchSreenshot();

return;

void TextSearch()
{
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
}

void SearchSreenshot()
{
    if (Environment.OSVersion.Version.Major >= 6)
        SetProcessDPIAware();

    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    var form = new SelectionForm();

    Rectangle region = new(450, 350, 350, 45);

    if (form.ShowDialog() == DialogResult.OK)
    {
        region = form.SelectedRegion;
        Console.WriteLine($"Selected area: {region}");
    }

    var checkInterval = TimeSpan.FromSeconds(1);
    var previousText = string.Empty;

    while (true)
    {
        using var bmp = CaptureScreenRegion(region);

        var text = GetTextFromBitmap(bmp);


        if (text != previousText && !string.IsNullOrWhiteSpace(text))
        {
            Console.WriteLine($"Read Text: '{text}'");
            previousText = text;

            var results = RunSearch(text);

            if (results == 0)
            {
                var retryBmp = CaptureScreenRegion(GetTranslatedRectangle(region));
                text = GetTextFromBitmap(retryBmp);

                results = RunSearch(text);
            }

            Console.WriteLine($"Read Text: '{text}'");
        }

        Thread.Sleep(checkInterval);
    }
}

List<UmaEvent> SearchEvents(string eventName)
{
    return db.Events
        .AsNoTracking() // loads the db entries as 'read only' -> changes wont be reflected in the db
        .WhereEventNameContains(eventName)
        .Include(e => e.Choices)
        .ThenInclude(c => c.Outcomes)
        .ToList();
}

int RunSearch(string eventName)
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

    return events.Count;
}

static Bitmap CaptureScreenRegion(Rectangle rect)
{
    if (rect.Width <= 0 || rect.Height <= 0)
        throw new ArgumentException("Rectangle width and height must be positive");

    var bmp = new Bitmap(rect.Width, rect.Height);
    using var g = Graphics.FromImage(bmp);

    g.CopyFromScreen(rect.Location, Point.Empty, rect.Size);

    return bmp;
}

static string GetTextFromBitmap(Bitmap bmp)
{
    var tessdataPath = Path.Combine(Directory.GetCurrentDirectory(), "tessdata");

    string text = "";

    using var ms = new MemoryStream();
    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
    ms.Position = 0;

    using var img = Pix.LoadFromMemory(ms.ToArray());

    using var engine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);

    using var page = engine.Process(img);

    text = page.GetText();

    return text.Trim();
}

Rectangle GetTranslatedRectangle(Rectangle region)
{
    const int pixelsToRemove = 40;
    region.X += pixelsToRemove;
    region.Width -= pixelsToRemove;

    return region;
}

[DllImport("user32.dll")]
extern static bool SetProcessDPIAware();