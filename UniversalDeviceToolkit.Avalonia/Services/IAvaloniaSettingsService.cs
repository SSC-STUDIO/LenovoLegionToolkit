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

public enum AvaloniaUpdateCheckStatus
{
    Success,
    RateLimitReached,
    Error,
    Unavailable,
}

public sealed record AvaloniaUpdateCheckResult(
    AvaloniaUpdateCheckStatus Status,
    string? LatestVersion = null);

public enum AvaloniaUpdateFeedbackKind
{
    NoUpdates,
    UpdateAvailable,
    RateLimitReached,
    Error,
}

public sealed record AvaloniaUpdateFeedbackState(
    AvaloniaUpdateFeedbackKind Kind,
    string TitleKey,
    string? MessageKey = null);

public static class AvaloniaUpdateFeedback
{
    public static AvaloniaUpdateFeedbackState Resolve(AvaloniaUpdateCheckResult result) =>
        result.Status switch
        {
            AvaloniaUpdateCheckStatus.Success when !string.IsNullOrWhiteSpace(result.LatestVersion) =>
                new(AvaloniaUpdateFeedbackKind.UpdateAvailable, "MainWindow_UpdateAvailable", "MainWindow_UpdateAvailableWithVersion"),
            AvaloniaUpdateCheckStatus.Success =>
                new(AvaloniaUpdateFeedbackKind.NoUpdates, "MainWindow_CheckForUpdates_Success_Title"),
            AvaloniaUpdateCheckStatus.RateLimitReached =>
                new(AvaloniaUpdateFeedbackKind.RateLimitReached, "MainWindow_CheckForUpdates_Error_Title", "MainWindow_CheckForUpdates_Error_ReachedRateLimit_Message"),
            _ =>
                new(AvaloniaUpdateFeedbackKind.Error, "MainWindow_CheckForUpdates_Error_Title", "MainWindow_CheckForUpdates_Error_Unknown_Message"),
        };
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
    string? ActionText = null,
    bool IsVisible = true);

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
    Task SetAccentColorAsync(string? hexColor);
    Task SetMultiSelectionAsync(string pageKey, string optionKey, IReadOnlyList<string> values);
    Task SetBootLogoAsync(string filePath);
    Task SetTextAsync(string pageKey, string optionKey, string? value);
    Task<AvaloniaUpdateCheckResult> CheckForUpdatesAsync();
    Task InvokeActionAsync(string pageKey, string optionKey);
    Task ExportSettingsAsync(string filePath);
    Task ImportSettingsAsync(string filePath);
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
