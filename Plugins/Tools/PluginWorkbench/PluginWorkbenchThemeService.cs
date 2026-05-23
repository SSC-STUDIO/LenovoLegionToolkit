using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using PluginTooling.Core;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace PluginWorkbench;

internal sealed class PluginWorkbenchThemeService
{
    private static readonly Uri[] HostDictionaryUris =
    [
        new("pack://application:,,,/Lenovo Legion Toolkit;component/Styles/DesignTokens.xaml", UriKind.Absolute),
        new("pack://application:,,,/Lenovo Legion Toolkit;component/Styles/AnimationTokens.xaml", UriKind.Absolute),
        new("pack://application:,,,/Lenovo Legion Toolkit;component/Styles/Animations.xaml", UriKind.Absolute),
        new("pack://application:,,,/Lenovo Legion Toolkit;component/Styles/ButtonStyles.xaml", UriKind.Absolute),
        new("pack://application:,,,/Lenovo Legion Toolkit;component/Styles/Typography.xaml", UriKind.Absolute),
        new("pack://application:,,,/Lenovo Legion Toolkit;component/Styles/Badge.xaml", UriKind.Absolute),
        new("pack://application:,,,/Lenovo Legion Toolkit;component/Styles/CardAction.xaml", UriKind.Absolute),
        new("pack://application:,,,/Lenovo Legion Toolkit;component/Styles/CardControl.xaml", UriKind.Absolute),
        new("pack://application:,,,/Lenovo Legion Toolkit;component/Styles/CardExpander.xaml", UriKind.Absolute),
        new("pack://application:,,,/Lenovo Legion Toolkit;component/Styles/DynamicScrollBar.xaml", UriKind.Absolute),
        new("pack://application:,,,/Lenovo Legion Toolkit;component/Styles/InfoBar.xaml", UriKind.Absolute),
        new("pack://application:,,,/Lenovo Legion Toolkit;component/Styles/NavigationStore.xaml", UriKind.Absolute),
    ];

    private readonly string _statePath;

    public PluginWorkbenchThemeService(string statePath)
    {
        _statePath = statePath;
    }

    public PluginWorkbenchUiState LoadState()
    {
        if (!File.Exists(_statePath))
            return new PluginWorkbenchUiState();

        try
        {
            var json = File.ReadAllText(_statePath);
            return JsonSerializer.Deserialize<PluginWorkbenchUiState>(json) ?? new PluginWorkbenchUiState();
        }
        catch
        {
            return new PluginWorkbenchUiState();
        }
    }

    public void SaveState(PluginWorkbenchUiState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);
        File.WriteAllText(_statePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
    }

    public ThemeApplyResult Apply(PluginWorkbenchThemeMode mode)
    {
        var isDark = mode switch
        {
            PluginWorkbenchThemeMode.Dark => true,
            PluginWorkbenchThemeMode.Light => false,
            _ => IsSystemDarkMode(),
        };

        var applicationTheme = isDark ? ApplicationTheme.Dark : ApplicationTheme.Light;
        ApplicationThemeManager.Apply(applicationTheme, WindowBackdropType.None, updateAccent: false);
        Application.Current.Resources["SnackbarShadowColor"] = isDark ? Colors.Black : Color.FromArgb(64, 0, 0, 0);

        var accent = SystemParameters.WindowGlassColor;
        ApplicationAccentColorManager.Apply(accent, accent, accent, accent);

        var modeLabel = mode == PluginWorkbenchThemeMode.System
            ? $"System ({(isDark ? "Dark" : "Light")})"
            : mode.ToString();

        if (!TryEnsureHostResources(out var hostResourceMessage))
        {
            return new ThemeApplyResult(
                false,
                $"{modeLabel} workbench theme active. Host resources unavailable: {hostResourceMessage}");
        }

        return new ThemeApplyResult(true, $"{modeLabel} host preview active");
    }

    private static bool IsSystemDarkMode()
    {
        try
        {
            using var personalize = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = personalize?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return true;
        }
    }

    private static bool TryEnsureHostResources(out string message)
    {
        try
        {
            foreach (var uri in HostDictionaryUris)
            {
                if (Application.Current.Resources.MergedDictionaries.Any(dictionary => uri.Equals(dictionary.Source)))
                    continue;

                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });
            }

            message = "Host resources loaded successfully.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Run Bootstrap Host and confirm Dependencies/Host matches the main app baseline. Details: {ex.Message}";
            return false;
        }
    }

    internal sealed record ThemeApplyResult(bool Success, string Message);
}
