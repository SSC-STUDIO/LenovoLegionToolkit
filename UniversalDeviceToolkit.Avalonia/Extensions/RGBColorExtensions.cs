using Avalonia.Media;
using UniversalDeviceToolkit.Lib;

namespace UniversalDeviceToolkit.Avalonia.Extensions;

public static class RGBColorExtensions
{
    public static Color ToColor(this RGBColor color) => Color.FromRgb(color.R, color.G, color.B);
}
