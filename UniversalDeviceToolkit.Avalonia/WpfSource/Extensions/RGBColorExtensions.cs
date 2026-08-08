using System.Windows.Media;
using UniversalDeviceToolkit.Lib;

namespace UniversalDeviceToolkit.WPF.Extensions;

public static class RGBColorExtensions
{
    public static Color ToColor(this RGBColor color) => Color.FromRgb(color.R, color.G, color.B);
}
