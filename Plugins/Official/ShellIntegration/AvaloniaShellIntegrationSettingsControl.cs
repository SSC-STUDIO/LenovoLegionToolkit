using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using UniversalDeviceToolkit.Plugins.SDK;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace UniversalDeviceToolkit.Plugins.ShellIntegration;

/// <summary>
/// Native Avalonia settings surface for Nilesoft Shell integration.  The
/// underlying plugin service remains shared with the WPF page, so registration
/// and managed-profile actions have identical behavior in both hosts.
/// </summary>
public sealed class AvaloniaShellIntegrationSettingsControl : UserControl
{
    private readonly ShellIntegrationPlugin _plugin;
    private readonly TextBlock _registration;
    private readonly TextBlock _version;
    private readonly TextBlock _path;
    private readonly TextBlock _status;
    private Button? _enableButton;
    private Button? _disableButton;
    private Button? _openStyleSettingsButton;
    private Button? _openShellFolderButton;
    private Button? _openConfigButton;
    private Button? _openManagedConfigButton;
    private Button? _syncManagedConfigButton;
    private Button? _resetManagedConfigButton;
    private Button? _importProfileButton;
    private Button? _applyDefaultPresetButton;
    private Button? _applyCompactDarkPresetButton;
    private Button? _applyMinimalLightPresetButton;

    public AvaloniaShellIntegrationSettingsControl(ShellIntegrationPlugin plugin)
    {
        _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        _registration = ValueText();
        _version = ValueText();
        _path = ValueText();
        _status = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Gray };
        AutomationProperties.SetAutomationId(this, "AvaloniaShellIntegrationSettingsRoot");
        AutomationProperties.SetAutomationId(_status, "AvaloniaShellIntegrationSettingsStatus");
        Content = BuildContent();
        Loaded += async (_, _) => await RefreshAsync().ConfigureAwait(true);
    }

    private Control BuildContent()
    {
        var root = new StackPanel { Spacing = 14, Margin = new Thickness(20, 16, 20, 20) };
        var summary = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*"), ColumnSpacing = 16 };
        var registrationMetric = Metric(ShellIntegrationText.RegistrationLabel, _registration);
        Grid.SetColumn(registrationMetric, 0);
        summary.Children.Add(registrationMetric);
        var versionMetric = Metric(ShellIntegrationText.VersionLabel, _version);
        Grid.SetColumn(versionMetric, 1);
        summary.Children.Add(versionMetric);
        var pathMetric = Metric(ShellIntegrationText.PathLabel, _path);
        Grid.SetColumn(pathMetric, 2);
        summary.Children.Add(pathMetric);
        root.Children.Add(Card(ShellIntegrationText.OverviewTitle, ShellIntegrationText.OverviewDescription, summary));

        var actions = new StackPanel { Spacing = 8 };
        var managementButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        _enableButton = ActionButton(ShellIntegrationText.EnableButton, EnableAsync, "AvaloniaShellIntegrationEnableButton");
        _disableButton = ActionButton(ShellIntegrationText.DisableButton, DisableAsync, "AvaloniaShellIntegrationDisableButton");
        _openStyleSettingsButton = ActionButton(ShellIntegrationText.OpenStyleSettingsButton, OpenStyleSettingsAsync, "AvaloniaShellIntegrationOpenStyleSettingsButton");
        _syncManagedConfigButton = ActionButton(ShellIntegrationText.SyncManagedConfigButton, SyncAsync, "AvaloniaShellIntegrationSyncButton");
        _resetManagedConfigButton = ActionButton(ShellIntegrationText.ResetManagedConfigButton, ResetAsync, "AvaloniaShellIntegrationResetButton");
        managementButtons.Children.Add(_enableButton);
        managementButtons.Children.Add(_disableButton);
        managementButtons.Children.Add(_openStyleSettingsButton);
        managementButtons.Children.Add(_syncManagedConfigButton);
        managementButtons.Children.Add(_resetManagedConfigButton);
        actions.Children.Add(managementButtons);

        var fileButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        _openShellFolderButton = ActionButton(ShellIntegrationText.OpenShellFolderButton, OpenFolder, "AvaloniaShellIntegrationOpenShellFolderButton");
        _openConfigButton = ActionButton(ShellIntegrationText.OpenConfigButton, OpenConfig, "AvaloniaShellIntegrationOpenConfigButton");
        _openManagedConfigButton = ActionButton(ShellIntegrationText.OpenManagedConfigButton, OpenManaged, "AvaloniaShellIntegrationOpenManagedConfigButton");
        fileButtons.Children.Add(_openShellFolderButton);
        fileButtons.Children.Add(_openConfigButton);
        fileButtons.Children.Add(_openManagedConfigButton);
        actions.Children.Add(fileButtons);
        root.Children.Add(Card(ShellIntegrationText.ActionsTitle, ShellIntegrationText.ActionsDescription, actions));

        var presets = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        _applyDefaultPresetButton = ActionButton(ShellIntegrationText.PresetDefaultButton, () => ApplyPresetAsync(ShellIntegrationPreset.Default), "AvaloniaShellIntegrationDefaultPresetButton");
        _applyCompactDarkPresetButton = ActionButton(ShellIntegrationText.PresetCompactDarkButton, () => ApplyPresetAsync(ShellIntegrationPreset.CompactDark), "AvaloniaShellIntegrationCompactDarkPresetButton");
        _applyMinimalLightPresetButton = ActionButton(ShellIntegrationText.PresetMinimalLightButton, () => ApplyPresetAsync(ShellIntegrationPreset.MinimalLight), "AvaloniaShellIntegrationMinimalLightPresetButton");
        var exportProfileButton = ActionButton(ShellIntegrationText.ExportProfileButton, ExportAsync, "AvaloniaShellIntegrationExportProfileButton");
        _importProfileButton = ActionButton(ShellIntegrationText.ImportProfileButton, ImportAsync, "AvaloniaShellIntegrationImportProfileButton");
        presets.Children.Add(_applyDefaultPresetButton);
        presets.Children.Add(_applyCompactDarkPresetButton);
        presets.Children.Add(_applyMinimalLightPresetButton);
        presets.Children.Add(exportProfileButton);
        presets.Children.Add(_importProfileButton);
        root.Children.Add(Card(ShellIntegrationText.PresetsTitle, ShellIntegrationText.PresetsDescription, presets));
        root.Children.Add(Card(ShellIntegrationText.StatusDetected, ShellIntegrationText.OptimizationHint, _status));
        return new ScrollViewer { Content = root, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    }

    private Task RefreshAsync(string? suffix = null, bool? isError = null)
    {
        var installed = _plugin.IsShellInstalled();
        var registered = installed && _plugin.IsShellRegistered();
        var shellFolder = _plugin.GetShellFolderPath();
        var shellConfigPath = _plugin.GetShellConfigPath();
        var configExists = !string.IsNullOrWhiteSpace(shellConfigPath) && File.Exists(shellConfigPath);
        var allowSystemActions = PluginHostContextRuntime.Current.AllowSystemActions;
        var isPreview = PluginHostContextRuntime.Current.Mode == UniversalDeviceToolkit.Lib.Plugins.PluginHostMode.Preview;
        var canManageShell = installed && allowSystemActions;
        var canManageConfig = installed;

        _registration.Text = registered
            ? ShellIntegrationText.RegisteredState
            : installed ? ShellIntegrationText.MissingState : ShellIntegrationText.NotFound;
        _version.Text = _plugin.GetShellVersion() ?? ShellIntegrationText.NotFound;
        _path.Text = _plugin.GetShellInstallPath() ?? ShellIntegrationText.NotFound;

        if (_enableButton is not null)
        {
            _enableButton.IsVisible = !registered;
            _enableButton.IsEnabled = canManageShell;
        }

        if (_disableButton is not null)
        {
            _disableButton.IsVisible = registered;
            _disableButton.IsEnabled = canManageShell;
        }

        if (_openStyleSettingsButton is not null)
            _openStyleSettingsButton.IsEnabled = canManageShell || isPreview;
        if (_openShellFolderButton is not null)
            _openShellFolderButton.IsEnabled = !string.IsNullOrWhiteSpace(shellFolder);
        if (_openConfigButton is not null)
            _openConfigButton.IsEnabled = configExists;
        if (_openManagedConfigButton is not null)
            _openManagedConfigButton.IsEnabled = canManageConfig;
        if (_syncManagedConfigButton is not null)
            _syncManagedConfigButton.IsEnabled = canManageConfig;
        if (_resetManagedConfigButton is not null)
            _resetManagedConfigButton.IsEnabled = canManageConfig;
        if (_importProfileButton is not null)
            _importProfileButton.IsEnabled = canManageConfig;
        if (_applyDefaultPresetButton is not null)
            _applyDefaultPresetButton.IsEnabled = canManageConfig;
        if (_applyCompactDarkPresetButton is not null)
            _applyCompactDarkPresetButton.IsEnabled = canManageConfig;
        if (_applyMinimalLightPresetButton is not null)
            _applyMinimalLightPresetButton.IsEnabled = canManageConfig;

        var prefix = registered
            ? ShellIntegrationText.StatusDetected
            : installed ? ShellIntegrationText.StatusRegistrationMissing : ShellIntegrationText.StatusNotDetected;
        var status = $"{prefix}\n{ShellIntegrationText.PathLabel}: {_path.Text}";
        if (!string.Equals(_version.Text, ShellIntegrationText.NotFound, StringComparison.Ordinal))
            status += $"\n{ShellIntegrationText.VersionLabel}: {_version.Text}";
        if (!allowSystemActions)
            status += "\nPreview mode: runtime actions are disabled.";
        if (!string.IsNullOrWhiteSpace(suffix))
            status += $"\n{suffix}";

        _status.Text = status;
        var effectiveIsError = isError ?? (!installed || !registered);
        _status.Foreground = effectiveIsError
            ? Brushes.IndianRed
            : Brushes.SeaGreen;
        return Task.CompletedTask;
    }

    private async Task EnableAsync()
    {
        var success = await _plugin.EnableShellAsync().ConfigureAwait(true);
        await RefreshAsync(
            success ? ShellIntegrationText.StatusEnableCompleted : ShellIntegrationText.StatusEnableFailed,
            !success).ConfigureAwait(true);
    }

    private async Task DisableAsync()
    {
        var success = await _plugin.DisableShellAsync().ConfigureAwait(true);
        await RefreshAsync(
            success ? ShellIntegrationText.StatusDisableCompleted : ShellIntegrationText.StatusDisableFailed,
            !success).ConfigureAwait(true);
    }

    private async Task SyncAsync()
    {
        var success = await _plugin.SyncManagedConfigurationAsync().ConfigureAwait(true);
        await RefreshAsync(
            success ? ShellIntegrationText.StatusManagedConfigSyncCompleted : ShellIntegrationText.StatusManagedConfigSyncFailed,
            !success).ConfigureAwait(true);
    }

    private async Task ResetAsync()
    {
        var success = await _plugin.ResetManagedConfigurationAsync().ConfigureAwait(true);
        await RefreshAsync(
            success ? ShellIntegrationText.StatusManagedConfigResetCompleted : ShellIntegrationText.StatusManagedConfigResetFailed,
            !success).ConfigureAwait(true);
    }

    private async Task ApplyPresetAsync(ShellIntegrationPreset preset)
    {
        var success = await _plugin.ApplyPresetAsync(preset).ConfigureAwait(true);
        await RefreshAsync(success
            ? preset switch
            {
                ShellIntegrationPreset.CompactDark => ShellIntegrationText.StatusPresetAppliedCompactDark,
                ShellIntegrationPreset.MinimalLight => ShellIntegrationText.StatusPresetAppliedMinimalLight,
                _ => ShellIntegrationText.StatusPresetAppliedDefault,
            }
            : ShellIntegrationText.StatusPresetApplyFailed,
            !success).ConfigureAwait(true);
    }

    private async void OpenFolder() => await RefreshAsync(
        _plugin.OpenShellFolder()
            ? ShellIntegrationText.StatusOpenedShellFolder
            : ShellIntegrationText.StatusShellFolderNotFound,
        !_plugin.IsShellInstalled()).ConfigureAwait(true);

    private async void OpenConfig() => await RefreshAsync(
        _plugin.OpenShellConfigFile()
            ? ShellIntegrationText.StatusOpenedConfig
            : ShellIntegrationText.StatusConfigNotFound,
        !File.Exists(_plugin.GetShellConfigPath() ?? string.Empty)).ConfigureAwait(true);

    private async void OpenManaged() => await RefreshAsync(
        _plugin.OpenManagedConfigFolder()
            ? ShellIntegrationText.StatusOpenedManagedConfig
            : ShellIntegrationText.StatusManagedConfigFolderUnavailable,
        !_plugin.IsShellInstalled()).ConfigureAwait(true);

    private async Task OpenStyleSettingsAsync()
    {
        var window = new Window
        {
            Title = ShellIntegrationText.SettingsPageTitle,
            Width = 760,
            Height = 620,
            MinWidth = 560,
            MinHeight = 420,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            FlowDirection = ResolveFlowDirection(),
            Content = new AvaloniaShellIntegrationStyleSettingsControl(_plugin),
        };
        if (TopLevel.GetTopLevel(this) is Window owner)
            await window.ShowDialog(owner).ConfigureAwait(true);
        else
            window.Show();

        await RefreshAsync(ShellIntegrationText.StatusOpenedStyleSettings, false).ConfigureAwait(true);
    }

    private async Task ExportAsync()
    {
        var file = await PickSaveFileAsync().ConfigureAwait(true);
        if (file is null)
        {
            return;
        }

        var success = _plugin.ExportProfile(file.Path.LocalPath, out var error);
        await RefreshAsync(
            success ? ShellIntegrationText.StatusProfileExportCompleted : ShellIntegrationText.StatusProfileExportFailed + " " + error,
            !success).ConfigureAwait(true);
    }

    private async Task ImportAsync()
    {
        var file = await PickOpenFileAsync().ConfigureAwait(true);
        if (file is null)
        {
            return;
        }

        var result = await _plugin.ImportProfileAsync(file.Path.LocalPath).ConfigureAwait(true);
        await RefreshAsync(
            result.Success ? ShellIntegrationText.StatusProfileImportCompleted : ShellIntegrationText.StatusProfileImportFailed + " " + result.Error,
            !result.Success).ConfigureAwait(true);
    }

    private async Task<IStorageFile?> PickSaveFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
            return null;
        var files = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = "shell-profile.json",
            FileTypeChoices = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }],
        });
        return files;
    }

    private async Task<IStorageFile?> PickOpenFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
            return null;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }],
        });
        return files.FirstOrDefault();
    }

    private static TextBlock ValueText() => new() { FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };

    private static Control Metric(string title, TextBlock value)
    {
        var panel = new StackPanel { Spacing = 3, MinWidth = 120 };
        panel.Children.Add(new TextBlock { Text = title, Foreground = Brushes.Gray, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(value);
        return panel;
    }

    private static Border Card(string title, string description, Control content)
    {
        var panel = new StackPanel { Spacing = 7 };
        panel.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new TextBlock { Text = description, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Gray });
        panel.Children.Add(content);
        return new Border
        {
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gray,
            Child = panel,
        };
    }

    private static Button ActionButton(string text, Func<Task> action, string? automationId = null)
    {
        var button = new Button { Content = text, Padding = new Thickness(12, 7), MinWidth = 120 };
        if (!string.IsNullOrWhiteSpace(automationId))
            AutomationProperties.SetAutomationId(button, automationId);
        ToolTip.SetTip(button, text);
        button.Click += async (_, _) => await action().ConfigureAwait(true);
        return button;
    }

    private static Button ActionButton(string text, Action action, string? automationId = null)
    {
        var button = new Button { Content = text, Padding = new Thickness(12, 7), MinWidth = 120 };
        if (!string.IsNullOrWhiteSpace(automationId))
            AutomationProperties.SetAutomationId(button, automationId);
        ToolTip.SetTip(button, text);
        button.Click += (_, _) => action();
        return button;
    }

    private static FlowDirection ResolveFlowDirection()
    {
        var culture = UniversalDeviceToolkit.Plugins.ShellIntegration.Resources.Resource.Culture
                      ?? CultureInfo.CurrentUICulture;
        return culture.TextInfo.IsRightToLeft
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;
    }
}

/// <summary>
/// Avalonia equivalent of the WPF style resource window. It exposes the same
/// packaged Shell paths without depending on WPF dialogs or resource themes.
/// </summary>
public sealed class AvaloniaShellIntegrationStyleSettingsControl : UserControl
{
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap };

    public AvaloniaShellIntegrationStyleSettingsControl(ShellIntegrationPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        var shellFolder = plugin.GetShellFolderPath();
        var configPath = plugin.GetShellConfigPath();
        var importsFolder = string.IsNullOrWhiteSpace(shellFolder) ? null : Path.Combine(shellFolder, "imports");
        var root = new StackPanel { Spacing = 10, Margin = new Thickness(20) };
        root.FlowDirection = ResolveFlowDirection();
        root.Children.Add(new TextBlock
        {
            Text = ShellIntegrationText.SettingsPageTitle,
            FontSize = 22,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
        root.Children.Add(_status);
        root.Children.Add(PathRow("shell.nss", configPath, ShellIntegrationText.OpenFileButton, false, "AvaloniaShellIntegrationOpenShellConfigFileButton"));
        root.Children.Add(PathRow("theme.nss", Join(importsFolder, "theme.nss"), ShellIntegrationText.OpenFileButton, false, "AvaloniaShellIntegrationOpenThemeFileButton"));
        root.Children.Add(PathRow("images.nss", Join(importsFolder, "images.nss"), ShellIntegrationText.OpenFileButton, false, "AvaloniaShellIntegrationOpenImagesFileButton"));
        root.Children.Add(PathRow("modify.nss", Join(importsFolder, "modify.nss"), ShellIntegrationText.OpenFileButton, false, "AvaloniaShellIntegrationOpenModifyFileButton"));
        root.Children.Add(PathRow("imports", importsFolder, ShellIntegrationText.OpenFolderButton, true, "AvaloniaShellIntegrationOpenImportsFolderButton"));
        root.Children.Add(PathRow("Shell Folder", shellFolder, ShellIntegrationText.OpenShellFolderButton, true, "AvaloniaShellIntegrationOpenShellFolderButton"));
        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = root,
        };
        AutomationProperties.SetAutomationId(this, "AvaloniaShellIntegrationStyleSettingsRoot");
    }

    private Border PathRow(string title, string? path, string actionLabel, bool directory, string automationId)
    {
        var value = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(path) ? ShellIntegrationText.NotFound : path,
            TextWrapping = TextWrapping.Wrap,
            Foreground = string.IsNullOrWhiteSpace(path) ? Brushes.IndianRed : Brushes.Gray,
        };
        var open = new Button { Content = actionLabel, MinWidth = 120, Padding = new Thickness(12, 7), IsEnabled = !string.IsNullOrWhiteSpace(path) };
        AutomationProperties.SetAutomationId(open, automationId);
        ToolTip.SetTip(open, actionLabel);
        open.Click += (_, _) => OpenPath(path, directory);
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        var copy = new StackPanel { Spacing = 4, MinWidth = 0 };
        copy.Children.Add(new TextBlock { Text = title, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
        copy.Children.Add(value);
        Grid.SetColumn(copy, 0);
        Grid.SetColumn(open, 1);
        grid.Children.Add(copy);
        grid.Children.Add(open);
        return new Border
        {
            Padding = new Thickness(14, 12),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gray,
            CornerRadius = new CornerRadius(8),
            Child = grid,
        };
    }

    private void OpenPath(string? path, bool directory)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        var exists = directory ? Directory.Exists(path) : File.Exists(path);
        if (!exists)
        {
            _status.Text = directory ? ShellIntegrationText.StatusShellFolderNotFound : ShellIntegrationText.StatusConfigNotFound;
            _status.Foreground = Brushes.IndianRed;
            return;
        }

        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        _status.Text = directory ? ShellIntegrationText.StatusOpenedShellFolder : ShellIntegrationText.StatusOpenedConfig;
        _status.Foreground = Brushes.SeaGreen;
    }

    private static string? Join(string? directory, string fileName) =>
        string.IsNullOrWhiteSpace(directory) ? null : Path.Combine(directory, fileName);

    private static FlowDirection ResolveFlowDirection()
    {
        var culture = UniversalDeviceToolkit.Plugins.ShellIntegration.Resources.Resource.Culture
                      ?? CultureInfo.CurrentUICulture;
        return culture.TextInfo.IsRightToLeft
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;
    }
}
