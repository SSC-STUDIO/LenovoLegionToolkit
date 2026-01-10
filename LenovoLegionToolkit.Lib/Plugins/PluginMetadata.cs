namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Plugin metadata, used to describe plugin information
/// </summary>
public class PluginMetadata
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsSystemPlugin { get; set; }
    public string[]? Dependencies { get; set; }
    public string Version { get; set; } = "1.0.0";
    public string MinimumHostVersion { get; set; } = "1.0.0";
    public string? Author { get; set; }
}

