using System.IO;
using System.Text.Json;

namespace PluginCompletionUiTool;

internal static class CompletionReportFile
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static async Task WriteAsync(string reportPath, CompletionReport report, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(report, JsonOptions), cancellationToken);
    }
}
