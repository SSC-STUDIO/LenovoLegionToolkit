namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Per-plugin language overrides consumed by the plugin store UI.
/// A null or empty value means the plugin follows the application language.
/// </summary>
public interface IPluginLanguageService
{
    string? GetLanguage(string pluginId);
    void SetLanguage(string pluginId, string? cultureName);
    event Action? LanguagesChanged;
}
