using LenovoLegionToolkit.Plugins.CustomMouse.Resources;

namespace LenovoLegionToolkit.Plugins.CustomMouse.Services;

/// <summary>
/// 自定义鼠标服务实现
/// </summary>
public class CustomMouseService : ICustomMouseService
{
    private readonly ILogger<CustomMouseService> _logger;
    private readonly string _cursorDirectory;
    private readonly List<MouseStyleInfo> _cachedStyles = new();

    public CustomMouseService(ILogger<CustomMouseService> logger)
    {
        _logger = logger;
        _cursorDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Microsoft",
            "Windows",
            "Cursors");
    }

    public Task<List<MouseStyleInfo>> GetAvailableStylesAsync()
    {
        _cachedStyles.Clear();
        
        // 添加内置样式
        _cachedStyles.Add(new MouseStyleInfo
        {
            Id = "W11-CC-V2.2-HDPI",
            Name = Resource.CustomMouse_Style_W11CC,
            Description = Resource.CustomMouse_Style_W11CC_Desc,
            Path = "Resources/W11-CC-V2.2-HDPI",
            IsInstalled = false,
            RequiresRestart = true
        });
        
        _cachedStyles.Add(new MouseStyleInfo
        {
            Id = "W11-CC-V2.2",
            Name = Resource.CustomMouse_Style_W11CC_Standard,
            Description = Resource.CustomMouse_Style_W11CC_Standard_Desc,
            Path = "Resources/W11-CC-V2.2",
            IsInstalled = false,
            RequiresRestart = true
        });
        
        return Task.FromResult(_cachedStyles);
    }

    public Task<bool> ApplyStyleAsync(string styleId)
    {
        _logger.LogInformation("Applying mouse style: {StyleId}", styleId);
        return Task.FromResult(true);
    }

    public Task<bool> RemoveStyleAsync(string styleId)
    {
        _logger.LogInformation("Removing mouse style: {StyleId}", styleId);
        return Task.FromResult(true);
    }

    public Task<bool> BackupCurrentStyleAsync()
    {
        _logger.LogInformation("Backing up current mouse style");
        return Task.FromResult(true);
    }

    public Task<bool> RestoreStyleAsync(string backupPath)
    {
        _logger.LogInformation("Restoring mouse style from: {Path}", backupPath);
        return Task.FromResult(true);
    }

    public Task<string> GetCurrentStyleAsync()
    {
        return Task.FromResult(string.Empty);
    }

    public Task<bool> IsStyleInstalledAsync(string styleId)
    {
        return Task.FromResult(false);
    }

    public Task RefreshStylesAsync()
    {
        _cachedStyles.Clear();
        return Task.CompletedTask;
    }
}
