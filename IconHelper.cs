using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;
using Svg.Skia;

namespace deepseek_copilot;

using Drawing = System.Drawing;

internal static class IconHelper
{
    private static SKPicture? _svgPicture;

    private static SKPicture LoadSvg()
    {
        if (_svgPicture != null) return _svgPicture;

        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("deepseek_copilot.copiloticon.svg");
        if (stream == null) throw new InvalidOperationException("Embedded SVG not found");

        var svg = new SKSvg();
        svg.Load(stream);
        _svgPicture = svg.Picture;
        return _svgPicture!;
    }

    public static BitmapSource RenderToBitmapSource(int size)
    {
        var picture = LoadSvg();
        using var bitmap = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        var cull = picture.CullRect;
        var scale = Math.Min(size / cull.Width, size / cull.Height);
        var fitW = cull.Width * scale;
        var fitH = cull.Height * scale;
        canvas.Translate((size - fitW) / 2, (size - fitH) / 2);
        canvas.Scale(scale, scale);
        canvas.DrawPicture(picture);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new MemoryStream(data.ToArray());

        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.StreamSource = ms;
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }

    public static Drawing.Icon RenderToIcon(int size = 32)
    {
        var picture = LoadSvg();
        using var bitmap = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        var cull = picture.CullRect;
        var scale = Math.Min(size / cull.Width, size / cull.Height);
        var fitW = cull.Width * scale;
        var fitH = cull.Height * scale;
        canvas.Translate((size - fitW) / 2, (size - fitH) / 2);
        canvas.Scale(scale, scale);
        canvas.DrawPicture(picture);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new MemoryStream(data.ToArray());
        using var bmp = new Drawing.Bitmap(ms);
        return Drawing.Icon.FromHandle(bmp.GetHicon());
    }
}
