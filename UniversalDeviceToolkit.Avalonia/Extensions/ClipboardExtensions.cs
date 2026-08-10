using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Avalonia.Utils;

namespace UniversalDeviceToolkit.Avalonia.Extensions;

public static class ClipboardExtensions
{
    public static void SetProcesses(IEnumerable<ProcessInfo> processes)
    {
        var sb = new StringBuilder();
        foreach (var process in processes)
            sb.AppendLine(process.ExecutablePath);
        UdtAppContext.Clipboard?.SetTextAsync(sb.ToString()).GetAwaiter().GetResult();
    }

    public static IEnumerable<ProcessInfo> GetProcesses() => (UdtAppContext.Clipboard?.GetTextAsync().GetAwaiter().GetResult() ?? string.Empty)
        .Split(Environment.NewLine)
        .Select(l => l.Trim('"'))
        .Where(File.Exists)
        .Distinct()
        .Select(ProcessInfo.FromPath);
}
