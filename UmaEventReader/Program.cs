// See https://aka.ms/new-console-template for more information
// todo: only print text when results are found
// maybe prefilter garbage that isnt text
// supress results with more than 10
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
    Rectangle region = new(450, 350, 330, 40);

    // var form = new SelectionForm();
    // if (form.ShowDialog() == DialogResult.OK)
    // {
        // region = form.SelectedRegion;
        // Console.WriteLine($"Selected area: {region}");
    // }

    var checkInterval = TimeSpan.FromSeconds(1);
    var previousText = string.Empty;

    while (true)
    {
        using var bmp = AddBorder(CaptureScreenRegion(region), 0, Color.White);
        var text = GetTextFromBitmap(bmp);

        if (text != previousText && !string.IsNullOrWhiteSpace(text))
        {
            Console.WriteLine($"Read Text: '{text}'");
            previousText = text;

            var results = RunSearch(text);

            if (results == 0)
            {
                var retryBmp = CaptureScreenRegion(region with { X = region.X - 40, Width = region.Width - 40 });
                text = GetTextFromBitmap(retryBmp);

                _ = RunSearch(text);
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

    var img = PixConverter.ToPix(bmp);

    using var engine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);

    engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,!?' ");
    engine.SetVariable("debug_file", "NUL");

    engine.DefaultPageSegMode = PageSegMode.SingleBlock;

    using var page = engine.Process(img);

    var text = page.GetText();

    return text.Trim();
}

Rectangle GetTranslatedRectangle(Rectangle region)
{
    var newRect = new Rectangle(region.X, region.Y, region.Width, region.Height);

    const int pixelsToRemove = 40;
    newRect.X += pixelsToRemove;
    newRect.Width -= pixelsToRemove;

    return newRect;
}

[DllImport("user32.dll")]
extern static bool SetProcessDPIAware();

Bitmap AddBorder(Bitmap src, int borderSize, Color borderColor)
{
    int newWidth = src.Width + borderSize * 2;
    int newHeight = src.Height + borderSize * 2;

    Bitmap bordered = new Bitmap(newWidth, newHeight);

    using (Graphics g = Graphics.FromImage(bordered))
    {
        // Fill background with border color
        g.Clear(borderColor);

        // Draw original image inside the border
        g.DrawImage(src, new Rectangle(borderSize, borderSize, src.Width, src.Height));
    }

    return bordered;
}