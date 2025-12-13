namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// 插件元数据，用于描述插件的信息
/// </summary>
public class PluginMetadata
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsSystemPlugin { get; set; }
    public string[]? Dependencies { get; set; }
}
