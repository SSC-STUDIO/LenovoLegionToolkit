using System.Windows.Media;
using LenovoLegionToolkit.Lib;

namespace UniversalDeviceToolkit.WPF.Extensions;

public static class ColorExtensions
{
    public static RGBColor ToRGBColor(this Color color) => new(color.R, color.G, color.B);
}