// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Tesseract;
using UmaEventReader;
using UmaEventReader.Extensions;
using UmaEventReader.Model;
using UmaEventReader.Services;
using ImageFormat = Tesseract.ImageFormat;
if (Environment.OSVersion.Version.Major >= 6)
{
    SetProcessDPIAware();
}

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
// {X=366,Y=269,Width=352,Height=56}
var form = new SelectionForm();

var region = new Rectangle(366, 269, 352, 56);

if (form.ShowDialog() == DialogResult.OK)
{
    region = form.SelectedRegion;
    Console.WriteLine($"Selected area: {region}");
}

using var bmp = CaptureScreenRegion(region);

var tempPath = Path.Combine(Path.GetTempPath(), $"bmp_{DateTime.Now:yyyyMMdd_HHmmssfff}.png");
bmp.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);

Console.WriteLine(GetTextFromBitmap(bmp));

var text = GetTextFromScreenRegion(region, "tessdata");

Console.Out.WriteLine(text);

// await DbInitializer.Initialize(true);
// await using var db = new UmaContext();

// _ = SearchEvents("pit"); // workaround for first search being slow
//
// var done = false;
//
// while (!done)
// {
//     Console.Out.Write("Event name: ");
//     var eventName = Console.ReadLine();
//
//     if (string.IsNullOrWhiteSpace(eventName))
//     {
//         done = true;
//         continue;
//     }
//
//     RunSearch(eventName);
// }


return;

// List<UmaEvent> SearchEvents(string eventName)
// {
//     return db.Events
//         .AsNoTracking() // loads the db entries as 'read only' -> changes wont be reflected in the db
//         .WhereEventNameContains(eventName)
//         .Include(e => e.Choices)
//         .ThenInclude(c => c.Outcomes)
//         .ToList();
// }
//
// void RunSearch(string eventName)
// {
//     Console.Out.WriteLine("Searching event name: " + eventName);
//
//     var sw = new Stopwatch();
//     sw.Start();
//
//     var events = SearchEvents(eventName);
//
//
//     var searchTime = sw.ElapsedMilliseconds;
//
//     foreach (var umaEvent in events)
//     {
//         Console.Out.WriteLine(umaEvent);
//
//         Console.Out.WriteLine("======================================");
//     }
//
//     Console.Out.WriteLine("Found " + events.Count + $" events in {searchTime}ms");
// }

static Bitmap CaptureScreenRegion(Rectangle rect)
{
    var bmp = new Bitmap(rect.Width, rect.Height);

    using (var g = Graphics.FromImage(bmp))
    {
        g.CopyFromScreen(rect.Location, Point.Empty, rect.Size);
    }

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

    return text;
}

static string GetTextFromScreenRegion(Rectangle region, string tessdataPath)
{
    // 1️⃣ Capture screen region
    Bitmap bmp = new Bitmap(region.Width, region.Height);

    using (Graphics g = Graphics.FromImage(bmp))
    {
        g.CopyFromScreen(region.Location, Point.Empty, region.Size);
    }

    // 2️⃣ Preprocess: grayscale
    Bitmap gray = new Bitmap(bmp.Width, bmp.Height);

    using (Graphics g = Graphics.FromImage(gray))
    {
        var cm = new ColorMatrix(new float[][]
        {
            [0.3f, 0.3f, 0.3f, 0, 0],
            [0.59f, 0.59f, 0.59f, 0, 0],
            [0.11f, 0.11f, 0.11f, 0, 0],
            [0, 0, 0, 1, 0],
            [0, 0, 0, 0, 1]
        });

        using var ia = new ImageAttributes();
        ia.SetColorMatrix(cm);

        g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height),
            0, 0, bmp.Width, bmp.Height,
            GraphicsUnit.Pixel, ia);
    }

    // 3️⃣ Optional thresholding (binary)
    for (int y = 0; y < gray.Height; y++)
    {
        for (int x = 0; x < gray.Width; x++)
        {
            Color pixel = gray.GetPixel(x, y);
            int value = pixel.R > 128 ? 255 : 0;
            gray.SetPixel(x, y, Color.FromArgb(value, value, value));
        }
    }

    var tempPath = Path.Combine(Path.GetTempPath(), $"ocr_capture_{DateTime.Now:yyyyMMdd_HHmmssfff}.png");
    gray.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
    Console.WriteLine($"Captured region saved to: {tempPath}");

    // 4️⃣ Run Tesseract OCR
    using var engine = new TesseractEngine(tessdataPath, "eng", EngineMode.LstmOnly)
    {
        DefaultPageSegMode = PageSegMode.SingleLine
    };

    string resultText;
    using var ms = new MemoryStream();
    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
    ms.Position = 0;

    using var pix = Pix.LoadFromMemory(ms.ToArray());

    using (var page = engine.Process(pix))
    {
        resultText = page.GetText();
    }

    gray.Dispose();
    bmp.Dispose();

    return resultText.Trim();
}

[DllImport("user32.dll")]
extern static bool SetProcessDPIAware();