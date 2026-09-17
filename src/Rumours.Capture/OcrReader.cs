using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Rumours.Core;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace Rumours.Capture;

public sealed class OcrReader
{
    private readonly OcrEngine _engine;

    public GameLanguage Language { get; }

    private OcrReader(GameLanguage language, OcrEngine engine) => (Language, _engine) = (language, engine);

    private static OcrReader? TryCreate(GameLocale locale)
    {
        var language = new Language(locale.OcrTag);
        return OcrEngine.IsLanguageSupported(language) && OcrEngine.TryCreateFromLanguage(language) is { } engine
            ? new OcrReader(locale.Language, engine)
            : null;
    }

    public static IReadOnlyList<OcrReader> ForGameLanguages()
    {
        var readers = GameLocales.All.Select(TryCreate).OfType<OcrReader>().ToList();
        if (readers.Count == 0)
            throw new InvalidOperationException(
                "Windows has no language pack with text recognition for the game language. " +
                "Add one: Settings → Time & language → Language & region.");
        return readers;
    }

    public IShotReader On(Bitmap shot) => new ShotReader(this, shot);

    private sealed class ShotReader(OcrReader reader, Bitmap shot) : IShotReader
    {
        public GameLanguage Language => reader.Language;

        public Task<IReadOnlyList<TextLine>> ReadAsync(Preprocessing preprocessing) => reader.ReadAsync(shot, preprocessing);
    }

    public async Task<IReadOnlyList<TextLine>> ReadAsync(Bitmap source, Preprocessing preprocessing)
    {
        using var enlarged = Enlarge(source, preprocessing, out var scale);
        using var software = await ToSoftwareBitmapAsync(enlarged);
        var result = await _engine.RecognizeAsync(software);

        return result.Lines.Select(line =>
        {
            var left = line.Words.Min(w => w.BoundingRect.Left);
            var top = line.Words.Min(w => w.BoundingRect.Top);
            var right = line.Words.Max(w => w.BoundingRect.Right);
            var bottom = line.Words.Max(w => w.BoundingRect.Bottom);
            return new TextLine(line.Text, left / scale, top / scale, (right - left) / scale, (bottom - top) / scale);
        }).ToList();
    }

    private static Bitmap Enlarge(Bitmap source, Preprocessing preprocessing, out double scale)
    {
        var limit = (int)OcrEngine.MaxImageDimension;
        scale = Math.Min(preprocessing.Upscale, Math.Min(limit / (double)source.Width, limit / (double)source.Height));
        var enlarged = new Bitmap((int)(source.Width * scale), (int)(source.Height * scale), PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(enlarged);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

        using var attributes = new ImageAttributes();
        if (preprocessing.HighContrastGrey) attributes.SetColorMatrix(HighContrastGrey);
        graphics.DrawImage(source, new Rectangle(0, 0, enlarged.Width, enlarged.Height),
            0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
        return enlarged;
    }

    private static readonly ColorMatrix HighContrastGrey = new(
    [
        [0.45f, 0.45f, 0.45f, 0, 0],
        [0.88f, 0.88f, 0.88f, 0, 0],
        [0.17f, 0.17f, 0.17f, 0, 0],
        [0, 0, 0, 1, 0],
        [-0.25f, -0.25f, -0.25f, 0, 1],
    ]);

    private static async Task<SoftwareBitmap> ToSoftwareBitmapAsync(Bitmap bitmap)
    {
        using var memory = new MemoryStream();
        bitmap.Save(memory, ImageFormat.Bmp);
        memory.Position = 0;

        using var stream = new InMemoryRandomAccessStream();
        await memory.CopyToAsync(stream.AsStreamForWrite());
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream);
        return await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
    }
}
