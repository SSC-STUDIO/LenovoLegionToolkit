using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

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

    public AvaloniaShellIntegrationSettingsControl(ShellIntegrationPlugin plugin)
    {
        _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        _registration = ValueText();
        _version = ValueText();
        _path = ValueText();
        _status = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Gray };
        AutomationProperties.SetAutomationId(this, "AvaloniaShellIntegrationSettingsRoot");
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
        actions.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                ActionButton(ShellIntegrationText.EnableButton, EnableAsync),
                ActionButton(ShellIntegrationText.DisableButton, DisableAsync),
                ActionButton(ShellIntegrationText.SyncManagedConfigButton, SyncAsync),
                ActionButton(ShellIntegrationText.ResetManagedConfigButton, ResetAsync),
            },
        });
        actions.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                ActionButton(ShellIntegrationText.OpenShellFolderButton, OpenFolder),
                ActionButton(ShellIntegrationText.OpenConfigButton, OpenConfig),
                ActionButton(ShellIntegrationText.OpenManagedConfigButton, OpenManaged),
            },
        });
        root.Children.Add(Card(ShellIntegrationText.ActionsTitle, ShellIntegrationText.ActionsDescription, actions));

        var presets = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                ActionButton(ShellIntegrationText.PresetDefaultButton, () => ApplyPresetAsync(ShellIntegrationPreset.Default)),
                ActionButton(ShellIntegrationText.PresetCompactDarkButton, () => ApplyPresetAsync(ShellIntegrationPreset.CompactDark)),
                ActionButton(ShellIntegrationText.PresetMinimalLightButton, () => ApplyPresetAsync(ShellIntegrationPreset.MinimalLight)),
                ActionButton(ShellIntegrationText.ExportProfileButton, ExportAsync),
                ActionButton(ShellIntegrationText.ImportProfileButton, ImportAsync),
            },
        };
        root.Children.Add(Card(ShellIntegrationText.PresetsTitle, ShellIntegrationText.PresetsDescription, presets));
        root.Children.Add(Card(ShellIntegrationText.StatusDetected, ShellIntegrationText.OptimizationHint, _status));
        return new ScrollViewer { Content = root, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    }

    private Task RefreshAsync()
    {
        var installed = _plugin.IsShellInstalled();
        var registered = installed && _plugin.IsShellRegistered();
        _registration.Text = registered
            ? ShellIntegrationText.RegisteredState
            : installed ? ShellIntegrationText.MissingState : ShellIntegrationText.NotFound;
        _version.Text = _plugin.GetShellVersion() ?? ShellIntegrationText.NotFound;
        _path.Text = _plugin.GetShellInstallPath() ?? ShellIntegrationText.NotFound;
        _status.Text = registered
            ? ShellIntegrationText.StatusDetected
            : installed ? ShellIntegrationText.StatusRegistrationMissing : ShellIntegrationText.StatusNotDetected;
        return Task.CompletedTask;
    }

    private async Task EnableAsync()
    {
        var success = await _plugin.EnableShellAsync().ConfigureAwait(true);
        _status.Text = success ? ShellIntegrationText.StatusEnableCompleted : ShellIntegrationText.StatusEnableFailed;
        await RefreshAsync().ConfigureAwait(true);
    }

    private async Task DisableAsync()
    {
        var success = await _plugin.DisableShellAsync().ConfigureAwait(true);
        _status.Text = success ? ShellIntegrationText.StatusDisableCompleted : ShellIntegrationText.StatusDisableFailed;
        await RefreshAsync().ConfigureAwait(true);
    }

    private async Task SyncAsync()
    {
        var success = await _plugin.SyncManagedConfigurationAsync().ConfigureAwait(true);
        _status.Text = success ? ShellIntegrationText.StatusManagedConfigSyncCompleted : ShellIntegrationText.StatusManagedConfigSyncFailed;
    }

    private async Task ResetAsync()
    {
        var success = await _plugin.ResetManagedConfigurationAsync().ConfigureAwait(true);
        _status.Text = success ? ShellIntegrationText.StatusManagedConfigResetCompleted : ShellIntegrationText.StatusManagedConfigResetFailed;
    }

    private async Task ApplyPresetAsync(ShellIntegrationPreset preset)
    {
        var success = await _plugin.ApplyPresetAsync(preset).ConfigureAwait(true);
        _status.Text = success
            ? preset switch
            {
                ShellIntegrationPreset.CompactDark => ShellIntegrationText.StatusPresetAppliedCompactDark,
                ShellIntegrationPreset.MinimalLight => ShellIntegrationText.StatusPresetAppliedMinimalLight,
                _ => ShellIntegrationText.StatusPresetAppliedDefault,
            }
            : ShellIntegrationText.StatusPresetApplyFailed;
    }

    private void OpenFolder() => _status.Text = _plugin.OpenShellFolder()
        ? ShellIntegrationText.StatusOpenedShellFolder : ShellIntegrationText.StatusShellFolderNotFound;

    private void OpenConfig() => _status.Text = _plugin.OpenShellConfigFile()
        ? ShellIntegrationText.StatusOpenedConfig : ShellIntegrationText.StatusConfigNotFound;

    private void OpenManaged() => _status.Text = _plugin.OpenManagedConfigFolder()
        ? ShellIntegrationText.StatusOpenedManagedConfig : ShellIntegrationText.StatusManagedConfigFolderUnavailable;

    private async Task ExportAsync()
    {
        var file = await PickSaveFileAsync().ConfigureAwait(true);
        if (file is null)
        {
            return;
        }

        var success = _plugin.ExportProfile(file.Path.LocalPath, out var error);
        _status.Text = success ? ShellIntegrationText.StatusProfileExportCompleted : ShellIntegrationText.StatusProfileExportFailed + " " + error;
    }

    private async Task ImportAsync()
    {
        var file = await PickOpenFileAsync().ConfigureAwait(true);
        if (file is null)
        {
            return;
        }

        var result = await _plugin.ImportProfileAsync(file.Path.LocalPath).ConfigureAwait(true);
        _status.Text = result.Success ? ShellIntegrationText.StatusProfileImportCompleted : ShellIntegrationText.StatusProfileImportFailed + " " + result.Error;
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

    private static Button ActionButton(string text, Func<Task> action)
    {
        var button = new Button { Content = text, Padding = new Thickness(12, 7), MinWidth = 120 };
        ToolTip.SetTip(button, text);
        button.Click += async (_, _) => await action().ConfigureAwait(true);
        return button;
    }

    private static Button ActionButton(string text, Action action)
    {
        var button = new Button { Content = text, Padding = new Thickness(12, 7), MinWidth = 120 };
        ToolTip.SetTip(button, text);
        button.Click += (_, _) => action();
        return button;
    }
}
