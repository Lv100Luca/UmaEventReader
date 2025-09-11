// See https://aka.ms/new-console-template for more information
// todo: only print text when results are found
// maybe prefilter garbage that isnt text
// supress results with more than 10
// use draw to pront debug things
// use draw to print outcomes to options
// todo: when event was successfully found -> save image under events as {eventname}.png
// will be used for testing later perhaps

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms.VisualStyles;
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

// var root = Path.Combine(AppContext.BaseDirectory, "captures");

await SearchSreenshot();

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

async Task SearchSreenshot()
{
    if (Environment.OSVersion.Version.Major >= 6)
        SetProcessDPIAware();

    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Rectangle region = new(322, 269, 411, 49);

    var form = new SelectionForm();

    // if (form.ShowDialog() == DialogResult.OK)
    // region = form.SelectedRegion;

    var offset = 60;

    var altRegion = region with
    {
        X = (region.X + offset), Y = region.Y, Width = (region.Width - offset), Height = region.Height
    };

    Console.WriteLine($"Selected area: {region}");
    Console.WriteLine($"Selected area alt : {altRegion}");

    // var t = new Thread(() =>
    // {
    // Application.EnableVisualStyles();
    // Application.Run(new OverlayForm([region, altRegion]));
    // });

    // t.SetApartmentState(ApartmentState.STA);
    // t.Start();

    var checkInterval = TimeSpan.FromSeconds(.5);
    var previousText = string.Empty;

    while (true)
    {
        // rework this
        // Console.Out.WriteLine("Searching");
        using var bmp = AddBorder(CaptureScreenRegion(region), 5, Color.Black);
        var test = ExtractWhiteText(bmp);

        test.Save("processed.png", ImageFormat.Png);

        var text = await GetTextFromBitmapAsync(test);
        // Console.Out.WriteLine($"'{text}'");

        bmp.Save("firstTry.png", ImageFormat.Png);

        if (text != previousText && !string.IsNullOrWhiteSpace(text) && text.Length > 3)
        {
            Console.WriteLine($"Read Text 1st: '{text}'");

            previousText = text;

            var results = RunSearch(text, bmp);

            if (results == 0)
            {
                var retryBmp = AddBorder(CaptureScreenRegion(altRegion), 5, Color.Black);
                var newTest = ExtractWhiteText(retryBmp);
                var newText = await GetTextFromBitmapAsync(newTest);

                retryBmp.Save("secondTry.png", ImageFormat.Png);

                // todo: make loop or smth
                if (newText != previousText && !string.IsNullOrWhiteSpace(newText) && text.Length > 3)
                {
                    text = newText;
                    Console.WriteLine($"Read Text 2nd: '{text}'");

                    results = RunSearch(text, retryBmp);
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

int RunSearch(string eventName, Bitmap? bmp = null)
{
    Console.Out.WriteLine("Searching event name: " + eventName);

    var sw = new Stopwatch();
    sw.Start();

    var events = SearchEvents(eventName);
    
    var searchTime = sw.ElapsedMilliseconds;

    if (events.Count == 1 && bmp != null)
        SaveEventImage(eventName, bmp);

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

async static Task<string> GetTextFromBitmapAsync(Bitmap bmp)
{
    var service = new TesseractTextExtractor();
    var tessdataPath = Path.Combine(Directory.GetCurrentDirectory(), "tessdata");

    // var img = PixConverter.ToPix(bmp);

    // using var engine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);

    // engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,!?'()#☆ ");
    // engine.SetVariable("debug_file", "NUL");

    // engine.DefaultPageSegMode = PageSegMode.SingleLine;

    // using var page = engine.Process(img);

    // var meanConfidence = page.GetMeanConfidence(); // 0.0 to 1.0
    // var text = page.GetText();

    var result = await service.ExtractTextAsync(bmp);

    var text = result.Text;
    var meanConfidence = result.Metadata?.MeanConfidence;

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

static Bitmap AddBorder(Bitmap src, int borderSize, Color borderColor)
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

static Bitmap ExtractWhiteText(Bitmap input, byte brightnessThreshold = 200)
{
    Bitmap output = new Bitmap(input.Width, input.Height);

    for (int y = 0; y < input.Height; y++)
    {
        for (int x = 0; x < input.Width; x++)
        {
            Color pixel = input.GetPixel(x, y);

            // Calculate brightness (0–255)
            byte brightness = (byte)((pixel.R + pixel.G + pixel.B) / 3);

            if (brightness >= brightnessThreshold)
            {
                // This was white text → after invert, make it black
                output.SetPixel(x, y, Color.Black);
            }
            else
            {
                // Background/shadow → after invert, make it white
                output.SetPixel(x, y, Color.White);
            }
        }
    }

    return output;
}


static void SaveEventImage(string eventName, Bitmap bmp)
{
    var root = Path.Combine(AppContext.BaseDirectory, "captures");

    // replace special chacaters
    var eventNameSanitized = MyRegex().Replace(eventName, "");

    Console.Out.WriteLine($"Saving Images to {root}");

    Directory.CreateDirectory(root); // make sure folder exists

    var filePath = Path.Combine(root, eventNameSanitized + ".png");

    if (File.Exists(filePath))
    {
        // already saved for this event, skip
        return;
    }

    bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
}

partial class Program
{
    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    private static partial Regex MyRegex();
}