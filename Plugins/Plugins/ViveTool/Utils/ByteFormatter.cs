using System;

namespace LenovoLegionToolkit.Plugins.ViveTool.Utils;

public static class ByteFormatter
{
    public static string FormatBytes(long bytes)
    {
        string[] suffix = { "B", "KB", "MB", "GB" };
        var value = (double)Math.Abs(bytes);
        var i = 0;

        while (i < suffix.Length - 1 && value >= 1024)
        {
            value /= 1024;
            i++;
        }

        if (bytes < 0)
        {
            value = -value;
        }

        return string.Format("{0:0.##} {1}", value, suffix[i]);
    }
}
