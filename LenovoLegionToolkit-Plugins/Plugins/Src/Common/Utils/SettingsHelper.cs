using System.Text.Json;

namespace LenovoLegionToolkit.Plugins.Common.Utils;

/// <summary>
/// Settings helper class for plugins
/// </summary>
public static class SettingsHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Save settings to a JSON file
    /// </summary>
    public static async Task SaveSettingsAsync<T>(string pluginId, T settings)
    {
        var settingsPath = GetSettingsPath(pluginId);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(settingsPath, json);
    }

    /// <summary>
    /// Load settings from a JSON file
    /// </summary>
    public static async Task<T?> LoadSettingsAsync<T>(string pluginId, T? defaultValue = default)
    {
        var settingsPath = GetSettingsPath(pluginId);
        
        if (!File.Exists(settingsPath))
            return defaultValue;

        try
        {
            var json = await File.ReadAllTextAsync(settingsPath);
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Delete settings file
    /// </summary>
    public static void DeleteSettings(string pluginId)
    {
        var settingsPath = GetSettingsPath(pluginId);
        if (File.Exists(settingsPath))
            File.Delete(settingsPath);
    }

    private static string GetSettingsPath(string pluginId)
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var pluginPath = Path.Combine(appDataPath, "Lenovo Legion Toolkit", "Plugins", pluginId);
        return Path.Combine(pluginPath, "settings.json");
    }
}
