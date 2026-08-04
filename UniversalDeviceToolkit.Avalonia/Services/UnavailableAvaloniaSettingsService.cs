namespace UniversalDeviceToolkit.Avalonia.Services;

internal sealed class UnavailableAvaloniaSettingsService : IAvaloniaSettingsService
{
    public Task<AvaloniaSettingsPageData> GetPageAsync(string pageKey)
    {
        var title = pageKey switch
        {
            "Application" => "Application Behavior",
            "Display" => "Display",
            "SmartKeys" => "Smart Keys",
            "Update" => "Update",
            "Power" => "Power",
            "Integrations" => "Integrations",
            _ => pageKey,
        };

        return Task.FromResult(new AvaloniaSettingsPageData(
            pageKey,
            title,
            "Settings are shown with the same controls as the Windows host.",
            Array.Empty<AvaloniaSettingOption>(),
            false,
            "This platform does not expose the device or application adapter required by this setting."));
    }

    public Task SetToggleAsync(string pageKey, string optionKey, bool value) =>
        Task.FromException(new PlatformNotSupportedException("Settings are unavailable on this host."));

    public Task SetSelectionAsync(string pageKey, string optionKey, string value) =>
        Task.FromException(new PlatformNotSupportedException("Settings are unavailable on this host."));

    public Task SetAccentColorAsync(string? hexColor) => Task.CompletedTask;

    public Task SetMultiSelectionAsync(string pageKey, string optionKey, IReadOnlyList<string> values) =>
        Task.FromException(new PlatformNotSupportedException("Settings are unavailable on this host."));

    public Task SetBootLogoAsync(string filePath) =>
        Task.FromException(new PlatformNotSupportedException("Boot logo controls are unavailable on this host."));

    public Task SetTextAsync(string pageKey, string optionKey, string? value) =>
        Task.FromException(new PlatformNotSupportedException("Settings are unavailable on this host."));

    public Task InvokeActionAsync(string pageKey, string optionKey) =>
        Task.FromException(new PlatformNotSupportedException("Settings are unavailable on this host."));

    public Task ExportSettingsAsync(string filePath) =>
        Task.FromException(new PlatformNotSupportedException("Settings backup is unavailable on this host."));

    public Task ImportSettingsAsync(string filePath) =>
        Task.FromException(new PlatformNotSupportedException("Settings backup is unavailable on this host."));
}
