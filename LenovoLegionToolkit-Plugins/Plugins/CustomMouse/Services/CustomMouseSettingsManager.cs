using LenovoLegionToolkit.Lib;

namespace LenovoLegionToolkit.Plugins.CustomMouse.Services;

/// <summary>
/// 自定义鼠标插件设置
/// </summary>
public class CustomMouseSettings
{
    /// <summary>
    /// 是否启用自动颜色检测
    /// </summary>
    public bool AutoColorDetectionEnabled { get; set; } = true;

    /// <summary>
    /// 当前选择的鼠标样式
    /// </summary>
    public string SelectedStyle { get; set; } = "W11-CC-V2.2-HDPI";

    /// <summary>
    /// 主题模式（auto/light/dark）
    /// </summary>
    public string ThemeMode { get; set; } = "auto";

    /// <summary>
    /// 亮度阈值（0.0-1.0）
    /// </summary>
    public double BrightnessThreshold { get; set; } = 0.5;

    /// <summary>
    /// 插件模式（extension/independent）
    /// </summary>
    public string PluginMode { get; set; } = "independent";

    /// <summary>
    /// 是否启用预览功能
    /// </summary>
    public bool EnablePreview { get; set; } = true;

    /// <summary>
    /// 是否自动应用样式
    /// </summary>
    public bool AutoApplyOnStartup { get; set; } = false;

    /// <summary>
    /// 上次检测的主题
    /// </summary>
    public string LastDetectedTheme { get; set; } = "";

    /// <summary>
    /// 是否显示提示信息
    /// </summary>
    public bool ShowNotifications { get; set; } = true;
}

/// <summary>
/// 自定义鼠标设置管理器
/// </summary>
public class CustomMouseSettingsManager
{
    private const string SettingsKey = "CustomMouseSettings";

    public CustomMouseSettings LoadSettings()
    {
        try
        {
            var settings = IoCContainer.Resolve<ApplicationSettings>();
            var settingsJson = settings.Store.GetCustomProperty(SettingsKey) as string;
            
            if (!string.IsNullOrEmpty(settingsJson))
            {
                try
                {
                    return System.Text.Json.JsonSerializer.Deserialize<CustomMouseSettings>(settingsJson) ?? new CustomMouseSettings();
                }
                catch
                {
                    // 如果反序列化失败，返回默认设置
                    return new CustomMouseSettings();
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to load custom mouse settings.", ex);
        }

        return new CustomMouseSettings();
    }

    public void SaveSettings(CustomMouseSettings settings)
    {
        try
        {
            var appSettings = IoCContainer.Resolve<ApplicationSettings>();
            var settingsJson = System.Text.Json.JsonSerializer.Serialize(settings);
            appSettings.Store.SetCustomProperty(SettingsKey, settingsJson);
            appSettings.Save();
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Custom mouse settings saved successfully");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to save custom mouse settings.", ex);
        }
    }

    public void ResetToDefault()
    {
        try
        {
            var appSettings = IoCContainer.Resolve<ApplicationSettings>();
            appSettings.Store.SetCustomProperty(SettingsKey, null);
            appSettings.Save();
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Custom mouse settings reset to default");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to reset custom mouse settings.", ex);
        }
    }
}