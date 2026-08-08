using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace UniversalDeviceToolkit.Avalonia.Extensions;

/// <summary>
/// Avalonia equivalent of the WPF image-source helpers. The associated-icon
/// extraction still uses GDI+ on Windows; the resource lookup switches to the
/// Avalonia asset loader so pack:// URIs are not required.
/// </summary>
public static class ImageSourceExtensions
{
    public static Bitmap? ApplicationIcon(string? exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath))
            return null;

        try
        {
#if WINDOWS
            using var icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
            if (icon is null)
                return null;
            using var bitmap = icon.ToBitmap();
            return ConvertDrawingBitmap(bitmap);
#else
            return null;
#endif
        }
        catch
        {
            return null;
        }
    }

    public static Bitmap FromResource(string name)
    {
        var uri = new Uri($"avares://udt-gui/{name}", UriKind.RelativeOrAbsolute);
        using var stream = AssetLoader.Open(uri);
        return new Bitmap(stream);
    }

#if WINDOWS
    private static Bitmap ConvertDrawingBitmap(System.Drawing.Bitmap source)
    {
        using var stream = new System.IO.MemoryStream();
        source.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;
        return new Bitmap(stream);
    }
#endif
}
