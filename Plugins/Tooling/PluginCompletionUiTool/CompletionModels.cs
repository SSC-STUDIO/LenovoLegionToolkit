using System.Text.Json.Serialization;

namespace PluginCompletionUiTool;

internal sealed class CompletionCheckRequest
{
    public string RepositoryRoot { get; init; } = string.Empty;

    public string Configuration { get; init; } = "Release";

    public bool SkipBuild { get; init; }

    public bool SkipTests { get; init; }

    public IReadOnlyList<string> PluginIds { get; init; } = Array.Empty<string>();
}

internal sealed class CompletionReport
{
    [JsonPropertyName("totals")]
    public CompletionTotals Totals { get; set; } = new();

    [JsonPropertyName("plugins")]
    public List<PluginReportItem> Plugins { get; set; } = [];

    [JsonPropertyName("steps")]
    public List<StepReportItem> Steps { get; set; } = [];
}

internal sealed class CompletionTotals
{
    [JsonPropertyName("pluginCount")]
    public int PluginCount { get; set; }

    [JsonPropertyName("failures")]
    public int Failures { get; set; }

    [JsonPropertyName("warnings")]
    public int Warnings { get; set; }
}

internal sealed class PluginReportItem
{
    [JsonPropertyName("pluginId")]
    public string PluginId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("failures")]
    public int Failures { get; set; }

    [JsonPropertyName("warnings")]
    public int Warnings { get; set; }
}

internal sealed class StepReportItem
{
    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("pluginId")]
    public string PluginId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
