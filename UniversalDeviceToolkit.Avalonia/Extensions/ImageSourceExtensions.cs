using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

namespace UniversalDeviceToolkit.Avalonia.Extensions;

public static class ImageSourceExtensions
{
    public static AvaloniaBitmap? ApplicationIcon(string? exePath)
    {
        if (exePath is null)
            return null;

        try
        {
            using var icon = Icon.ExtractAssociatedIcon(exePath);
            if (icon is null)
                return null;

            using var bitmap = icon.ToBitmap();
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            stream.Position = 0;
            return new AvaloniaBitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    public static AvaloniaBitmap FromResource(string name)
    {
        var path = "avares://UniversalDeviceToolkit.Avalonia/" + name;
        return new AvaloniaBitmap(path);
    }
}
