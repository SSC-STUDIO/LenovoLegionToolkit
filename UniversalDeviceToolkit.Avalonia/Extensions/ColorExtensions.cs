using Avalonia.Media;
using UniversalDeviceToolkit.Lib;

namespace UniversalDeviceToolkit.Avalonia.Extensions;

public static class ColorExtensions
{
    public static RGBColor ToRGBColor(this Color color) => new(color.R, color.G, color.B);
}
