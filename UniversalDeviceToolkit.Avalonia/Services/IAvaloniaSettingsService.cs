using System.Collections.Generic;
using System.Threading.Tasks;

namespace UniversalDeviceToolkit.Avalonia.Services;

public enum AvaloniaSettingEditor
{
    Toggle,
    Selection,
    MultiSelection,
    Text,
    Action,
}

public sealed record AvaloniaSettingOption(
    string Key,
    string Title,
    string Description,
    AvaloniaSettingEditor Editor,
    bool IsEnabled,
    bool BoolValue = false,
    string? TextValue = null,
    IReadOnlyList<string>? Values = null,
    string? SelectedValue = null,
    IReadOnlyList<string>? SelectedValues = null,
    string? Warning = null,
    string? ActionText = null);

public sealed record AvaloniaSettingsPageData(
    string PageKey,
    string Title,
    string Description,
    IReadOnlyList<AvaloniaSettingOption> Options,
    bool IsAvailable,
    string? UnavailableReason = null);

/// <summary>
/// Small UI-facing settings contract shared by Avalonia pages. The Windows implementation
/// delegates to the existing Lib settings stores; portable hosts return explicit unavailable
/// state while keeping the same page and control structure.
/// </summary>
public interface IAvaloniaSettingsService
{
    Task<AvaloniaSettingsPageData> GetPageAsync(string pageKey);
    Task SetToggleAsync(string pageKey, string optionKey, bool value);
    Task SetSelectionAsync(string pageKey, string optionKey, string value);
    Task SetMultiSelectionAsync(string pageKey, string optionKey, IReadOnlyList<string> values);
    Task SetTextAsync(string pageKey, string optionKey, string? value);
    Task InvokeActionAsync(string pageKey, string optionKey);
}

public static class AvaloniaSettingsServiceFactory
{
    public static IAvaloniaSettingsService Create() =>
#if WINDOWS
        new WindowsAvaloniaSettingsService();
#else
        new UnavailableAvaloniaSettingsService();
#endif
}
