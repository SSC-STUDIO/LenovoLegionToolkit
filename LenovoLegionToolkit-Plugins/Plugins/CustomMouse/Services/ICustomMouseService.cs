namespace LenovoLegionToolkit.Plugins.CustomMouse.Services;

/// <summary>
/// 鼠标样式信息
/// </summary>
public class MouseStyleInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsInstalled { get; set; }
    public bool RequiresRestart { get; set; }
}

/// <summary>
/// 自定义鼠标服务接口
/// </summary>
public interface ICustomMouseService
{
    Task<List<MouseStyleInfo>> GetAvailableStylesAsync();
    Task<bool> ApplyStyleAsync(string styleId);
    Task<bool> RemoveStyleAsync(string styleId);
    Task<bool> BackupCurrentStyleAsync();
    Task<bool> RestoreStyleAsync(string backupPath);
    Task<string> GetCurrentStyleAsync();
    Task<bool> IsStyleInstalledAsync(string styleId);
    Task RefreshStylesAsync();
}
