// See https://aka.ms/new-console-template for more information
// todo: only print text when results are found
// maybe prefilter garbage that isnt text
// supress results with more than 10
// use draw to pront debug things
// use draw to print outcomes to options

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Tesseract;
using UmaEventReader;
using UmaEventReader.Extensions;
using UmaEventReader.Model;
using UmaEventReader.Services;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

await using var db = new UmaContext();
// await DbInitializer.Initialize(true);

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
    Rectangle region = new(400, 349, 350, 48);

    var form = new SelectionForm();

    // if (form.ShowDialog() == DialogResult.OK)
    // region = form.SelectedRegion;

    var offset = 50;

    var altRegion = region with
    {
        X = (region.X + offset), Y = region.Y, Width = (region.Width - offset), Height = region.Height
    };

    Console.WriteLine($"Selected area: {region}");
    Console.WriteLine($"Selected area alt : {altRegion}");

    // var t = new Thread(() =>
    // {
    //     Application.EnableVisualStyles();
    //     Application.Run(new OverlayForm([region, altRegion]));
    // });
    //
    // t.SetApartmentState(ApartmentState.STA);
    // t.Start();

    var checkInterval = TimeSpan.FromSeconds(1);
    var previousText = string.Empty;

    while (true)
    {
        // rework this
        // Console.Out.WriteLine("Searching");
        using var bmp = CaptureScreenRegion(region);
        var text = GetTextFromBitmap(bmp);
        Console.Out.WriteLine($"'{text}'");

        bmp.Save("firstTry.png", ImageFormat.Png);

        if (text != previousText && !string.IsNullOrWhiteSpace(text) && text.Length > 3)
        {
            Console.WriteLine($"Read Text 1st: '{text}'");

            previousText = text;

            var results = RunSearch(text);

            if (results == 0)
            {
                var retryBmp = CaptureScreenRegion(altRegion);
                var newText = GetTextFromBitmap(retryBmp);

                retryBmp.Save("secondTry.png", ImageFormat.Png);

                // todo: make loop or smth
                if (newText != previousText && !string.IsNullOrWhiteSpace(newText) && text.Length > 3)
                {
                    text = newText;
                    Console.WriteLine($"Read Text 2nd: '{text}'");

                    results = RunSearch(text);
                }
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

    // only take 5 events should there be more
    foreach (var umaEvent in events.Take(5))
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

    engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,!?'()# ");
    engine.SetVariable("debug_file", "NUL");

    engine.DefaultPageSegMode = PageSegMode.SingleLine;

    using var page = engine.Process(img);

    var meanConfidence = page.GetMeanConfidence(); // 0.0 to 1.0
    var text = page.GetText();

    var confidenceThreshold = 0.6;

    if (meanConfidence < confidenceThreshold)
    {
        Console.Out.WriteLine($"Not confident in '{text}' ({meanConfidence})");

        return string.Empty;
    }

    // some filtering steps

    // sometimes a starting ! is read (wrong) -> replace it
    if (text.StartsWith('!') || text.StartsWith('.'))
        text = text[1..];

    //replace trailing new line
    text = text.Replace("\n", "");

    return text.Trim();
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