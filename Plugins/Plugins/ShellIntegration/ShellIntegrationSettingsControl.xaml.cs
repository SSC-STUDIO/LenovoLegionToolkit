using System;
using System.IO;
using System.Windows;
#nullable enable

using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using LenovoLegionToolkit.Plugins.Shared;

namespace LenovoLegionToolkit.Plugins.ShellIntegration;

public partial class ShellIntegrationSettingsControl : UserControl
{
    private readonly ShellIntegrationPlugin _plugin;

    public ShellIntegrationSettingsControl(ShellIntegrationPlugin plugin)
    {
        _plugin = plugin;
        WpfFallbackHelper.TryInitializeComponent(this, BuildFallbackUi);
        RefreshStatus();
    }

    private void BuildFallbackUi()
    {
        _statusTextBlock = new TextBlock
        {
            Margin = new Thickness(0, 12, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        AutomationProperties.SetAutomationId(_statusTextBlock, "ShellIntegration_StatusText");

        var root = new Grid { Margin = new Thickness(16) };
        AutomationProperties.SetAutomationId(root, "ShellIntegrationSettingsRoot");
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var subtitle = new TextBlock
        {
            Text = ShellIntegrationText.Subtitle,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(subtitle, 0);
        root.Children.Add(subtitle);

        Grid.SetRow(_statusTextBlock, 1);
        root.Children.Add(_statusTextBlock);

        var buttonPanel = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        _enableButton = new Wpf.Ui.Controls.Button { Content = ShellIntegrationText.EnableButton, Width = 90 };
        AutomationProperties.SetAutomationId(_enableButton, "EnableButton");
        _enableButton.Click += EnableButton_Click;
        _disableButton = new Wpf.Ui.Controls.Button { Content = ShellIntegrationText.DisableButton, Width = 90, Margin = new Thickness(8, 0, 0, 0) };
        AutomationProperties.SetAutomationId(_disableButton, "DisableButton");
        _disableButton.Click += DisableButton_Click;
        _openStyleSettingsButton = new Wpf.Ui.Controls.Button { Content = ShellIntegrationText.OpenStyleShortButton, Width = 120, Margin = new Thickness(8, 0, 0, 0) };
        AutomationProperties.SetAutomationId(_openStyleSettingsButton, "OpenStyleSettingsButton");
        _openStyleSettingsButton.Click += OpenStyleButton_Click;
        _openShellFolderButton = new Wpf.Ui.Controls.Button { Content = ShellIntegrationText.OpenShellFolderButton, Width = 160, Margin = new Thickness(8, 0, 0, 0) };
        AutomationProperties.SetAutomationId(_openShellFolderButton, "OpenShellFolderButton");
        _openShellFolderButton.Click += OpenShellFolderButton_Click;
        _openConfigButton = new Wpf.Ui.Controls.Button { Content = ShellIntegrationText.OpenConfigButton, Width = 140, Margin = new Thickness(8, 0, 0, 0) };
        AutomationProperties.SetAutomationId(_openConfigButton, "OpenConfigButton");
        _openConfigButton.Click += OpenConfigButton_Click;
        _syncManagedConfigButton = new Wpf.Ui.Controls.Button { Content = ShellIntegrationText.SyncManagedConfigButton, Width = 160, Margin = new Thickness(8, 0, 0, 0) };
        AutomationProperties.SetAutomationId(_syncManagedConfigButton, "SyncManagedConfigButton");
        _syncManagedConfigButton.Click += SyncManagedConfigButton_Click;
        _resetManagedConfigButton = new Wpf.Ui.Controls.Button { Content = ShellIntegrationText.ResetManagedConfigButton, Width = 170, Margin = new Thickness(8, 0, 0, 0) };
        AutomationProperties.SetAutomationId(_resetManagedConfigButton, "ResetManagedConfigButton");
        _resetManagedConfigButton.Click += ResetManagedConfigButton_Click;
        _openManagedConfigButton = new Wpf.Ui.Controls.Button { Content = ShellIntegrationText.OpenManagedConfigButton, Width = 170, Margin = new Thickness(8, 0, 0, 0) };
        AutomationProperties.SetAutomationId(_openManagedConfigButton, "OpenManagedConfigButton");
        _openManagedConfigButton.Click += OpenManagedConfigButton_Click;
        _exportProfileButton = new Wpf.Ui.Controls.Button { Content = ShellIntegrationText.ExportProfileButton, Width = 140, Margin = new Thickness(8, 0, 0, 0) };
        AutomationProperties.SetAutomationId(_exportProfileButton, "ExportProfileButton");
        _exportProfileButton.Click += ExportProfileButton_Click;
        _importProfileButton = new Wpf.Ui.Controls.Button { Content = ShellIntegrationText.ImportProfileButton, Width = 140, Margin = new Thickness(8, 0, 0, 0) };
        AutomationProperties.SetAutomationId(_importProfileButton, "ImportProfileButton");
        _importProfileButton.Click += ImportProfileButton_Click;
        _applyDefaultPresetButton = new Wpf.Ui.Controls.Button { Content = ShellIntegrationText.PresetDefaultButton, Width = 140, Margin = new Thickness(8, 0, 0, 0) };
        AutomationProperties.SetAutomationId(_applyDefaultPresetButton, "ApplyDefaultPresetButton");
        _applyDefaultPresetButton.Click += ApplyDefaultPresetButton_Click;
        _applyCompactDarkPresetButton = new Wpf.Ui.Controls.Button { Content = ShellIntegrationText.PresetCompactDarkButton, Width = 160, Margin = new Thickness(8, 0, 0, 0) };
        AutomationProperties.SetAutomationId(_applyCompactDarkPresetButton, "ApplyCompactDarkPresetButton");
        _applyCompactDarkPresetButton.Click += ApplyCompactDarkPresetButton_Click;
        _applyMinimalLightPresetButton = new Wpf.Ui.Controls.Button { Content = ShellIntegrationText.PresetMinimalLightButton, Width = 160, Margin = new Thickness(8, 0, 0, 0) };
        AutomationProperties.SetAutomationId(_applyMinimalLightPresetButton, "ApplyMinimalLightPresetButton");
        _applyMinimalLightPresetButton.Click += ApplyMinimalLightPresetButton_Click;
        buttonPanel.Children.Add(_enableButton);
        buttonPanel.Children.Add(_disableButton);
        buttonPanel.Children.Add(_openStyleSettingsButton);
        buttonPanel.Children.Add(_openShellFolderButton);
        buttonPanel.Children.Add(_openConfigButton);
        buttonPanel.Children.Add(_syncManagedConfigButton);
        buttonPanel.Children.Add(_resetManagedConfigButton);
        buttonPanel.Children.Add(_openManagedConfigButton);
        buttonPanel.Children.Add(_exportProfileButton);
        buttonPanel.Children.Add(_importProfileButton);
        buttonPanel.Children.Add(_applyDefaultPresetButton);
        buttonPanel.Children.Add(_applyCompactDarkPresetButton);
        buttonPanel.Children.Add(_applyMinimalLightPresetButton);

        Grid.SetRow(buttonPanel, 2);
        root.Children.Add(buttonPanel);

        Content = root;
    }

    private void RefreshStatus(string? suffix = null, bool? isError = null)
    {
        if (_statusTextBlock is null)
        {
            return;
        }

        var allowSystemActions = LenovoLegionToolkit.Plugins.SDK.PluginHostContext.Current.AllowSystemActions;
        var installed = _plugin.IsShellInstalled();
        var shellFolder = _plugin.GetShellFolderPath();
        var shellConfigPath = _plugin.GetShellConfigPath();
        var configExists = !string.IsNullOrWhiteSpace(shellConfigPath) && File.Exists(shellConfigPath);
        var configPath = configExists ? shellConfigPath! : ShellIntegrationText.NotFound;
        var version = _plugin.GetShellVersion() ?? ShellIntegrationText.NotFound;
        var path = _plugin.GetShellInstallPath() ?? ShellIntegrationText.NotFound;
        var isRegistered = installed && IsShellCurrentlyRegistered();
        var prefix = !installed
            ? ShellIntegrationText.StatusNotDetected
            : isRegistered
                ? ShellIntegrationText.StatusDetected
                : ShellIntegrationText.StatusRegistrationMissing;
        var canManageShell = installed && allowSystemActions;
        var canOpenShellFolder = !string.IsNullOrWhiteSpace(shellFolder);
        var canOpenConfig = configExists;
        var canManageConfig = installed;
        var canOpenStyleSettings = canManageShell || LenovoLegionToolkit.Plugins.SDK.PluginHostContext.Current.Mode == LenovoLegionToolkit.Plugins.SDK.PluginHostMode.Preview;

        _registrationValueTextBlock?.Text = installed
                ? (isRegistered ? ShellIntegrationText.RegisteredState : ShellIntegrationText.MissingState)
                : ShellIntegrationText.MissingState;

        _versionValueTextBlock?.Text = version;

        _configValueTextBlock?.Text = configPath;

        _pathValueTextBlock?.Text = path;

        // Toggle Enable/Disable visibility based on registration state
        if (_enableButton != null)
        {
            _enableButton.IsEnabled = canManageShell;
            _enableButton.Visibility = isRegistered ? Visibility.Collapsed : Visibility.Visible;
        }

        if (_disableButton != null)
        {
            _disableButton.IsEnabled = canManageShell;
            _disableButton.Visibility = isRegistered ? Visibility.Visible : Visibility.Collapsed;
        }

        _openStyleSettingsButton?.IsEnabled = canOpenStyleSettings;

        _openShellFolderButton?.IsEnabled = canOpenShellFolder;

        _openConfigButton?.IsEnabled = canOpenConfig;

        _syncManagedConfigButton?.IsEnabled = canManageConfig;

        _resetManagedConfigButton?.IsEnabled = canManageConfig;

        _openManagedConfigButton?.IsEnabled = canManageConfig;

        _exportProfileButton?.IsEnabled = true;

        _importProfileButton?.IsEnabled = canManageConfig;

        _applyDefaultPresetButton?.IsEnabled = canManageConfig;

        _applyCompactDarkPresetButton?.IsEnabled = canManageConfig;

        _applyMinimalLightPresetButton?.IsEnabled = canManageConfig;

        _statusTextBlock.Text = $"{prefix}\n{ShellIntegrationText.PathLabel}: {path}";
        if (!string.IsNullOrWhiteSpace(version) && version != ShellIntegrationText.NotFound)
        {
            _statusTextBlock.Text += $"\n{ShellIntegrationText.VersionLabel}: {version}";
        }

        if (!allowSystemActions)
        {
            _statusTextBlock.Text += "\nPreview mode: runtime actions are disabled.";
        }

        if (!string.IsNullOrWhiteSpace(suffix))
        {
            _statusTextBlock.Text += $"\n{suffix}";
        }

        var effectiveIsError = isError ?? !installed || !isRegistered;
        _statusTextBlock.Foreground = effectiveIsError
            ? ResolveBrush("SystemFillColorCriticalBrush", SystemColors.ControlTextBrush)
            : ResolveBrush("SystemFillColorSuccessBrush", SystemColors.ControlTextBrush);

        if (_statusIcon is not null)
        {
            _statusIcon.Symbol = effectiveIsError
                ? Wpf.Ui.Controls.SymbolRegular.ErrorCircle24
                : Wpf.Ui.Controls.SymbolRegular.CheckmarkCircle24;
            _statusIcon.Foreground = _statusTextBlock.Foreground;
        }
    }

    private static Brush ResolveBrush(string resourceKey, Brush fallback)
    {
        return Application.Current?.TryFindResource(resourceKey) as Brush ?? fallback;
    }

    private bool IsShellCurrentlyRegistered()
    {
        try
        {
            return _plugin.IsShellRegistered();
        }
        catch
        {
            return false;
        }
    }

    private async void EnableButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var success = await _plugin.EnableShellAsync().ConfigureAwait(true);
            RefreshStatus(success ? ShellIntegrationText.StatusEnableCompleted : ShellIntegrationText.StatusEnableFailed, !success);
        }
        catch (Exception ex)
        {
            RefreshStatus($"{ShellIntegrationText.ErrorPrefix}: {ex.Message}", true);
            PluginLog.Trace($"EnableButton_Click error: {ex.Message}", ex);
        }
    }

    private async void DisableButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var success = await _plugin.DisableShellAsync().ConfigureAwait(true);
            RefreshStatus(success ? ShellIntegrationText.StatusDisableCompleted : ShellIntegrationText.StatusDisableFailed, !success);
        }
        catch (Exception ex)
        {
            RefreshStatus($"{ShellIntegrationText.ErrorPrefix}: {ex.Message}", true);
            PluginLog.Trace($"DisableButton_Click error: {ex.Message}", ex);
        }
    }

    private void OpenStyleButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _plugin.OpenStyleSettingsWindow();
            RefreshStatus(ShellIntegrationText.StatusOpenedStyleSettings, false);
        }
        catch (Exception ex)
        {
            RefreshStatus($"{ShellIntegrationText.ErrorPrefix}: {ex.Message}", true);
            PluginLog.Trace($"OpenStyleButton_Click error: {ex.Message}", ex);
        }
    }

    private void OpenShellFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var success = _plugin.OpenShellFolder();
        RefreshStatus(success ? ShellIntegrationText.StatusOpenedShellFolder : ShellIntegrationText.StatusShellFolderNotFound, !success);
    }

    private void OpenConfigButton_Click(object sender, RoutedEventArgs e)
    {
        var success = _plugin.OpenShellConfigFile();
        RefreshStatus(success ? ShellIntegrationText.StatusOpenedConfig : ShellIntegrationText.StatusConfigNotFound, !success);
    }

    private void OpenManagedConfigButton_Click(object sender, RoutedEventArgs e)
    {
        var success = _plugin.OpenManagedConfigFolder();
        RefreshStatus(success ? ShellIntegrationText.StatusOpenedManagedConfig : ShellIntegrationText.StatusManagedConfigFolderUnavailable, !success);
    }

    private void SyncManagedConfigButton_Click(object sender, RoutedEventArgs e)
    {
        var success = Task.Run(async () => await _plugin.SyncManagedConfigurationAsync()).GetAwaiter().GetResult();
        RefreshStatus(success ? ShellIntegrationText.StatusManagedConfigSyncCompleted : ShellIntegrationText.StatusManagedConfigSyncFailed, !success);
    }

    private void ResetManagedConfigButton_Click(object sender, RoutedEventArgs e)
    {
        var success = _plugin.ResetManagedConfiguration();
        RefreshStatus(success ? ShellIntegrationText.StatusManagedConfigResetCompleted : ShellIntegrationText.StatusManagedConfigResetFailed, !success);
    }

    private void ExportProfileButton_Click(object sender, RoutedEventArgs e)
    {
        var saveFileDialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = ShellIntegrationText.ExportProfileButton,
            Filter = ShellIntegrationText.ProfileFileDialogFilter,
            DefaultExt = ".json",
            FileName = "shell-integration-profile.json",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (saveFileDialog.ShowDialog() != true)
        {
            return;
        }

        var success = _plugin.ExportProfile(saveFileDialog.FileName, out var errorMessage);
        RefreshStatus(
            success ? ShellIntegrationText.StatusProfileExportCompleted : $"{ShellIntegrationText.StatusProfileExportFailed} {errorMessage}".Trim(),
            !success);
    }

    private void ImportProfileButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = ShellIntegrationText.ImportProfileButton,
            Filter = ShellIntegrationText.ProfileFileDialogFilter,
            FilterIndex = 1
        };

        if (openFileDialog.ShowDialog() != true)
        {
            return;
        }

        var success = _plugin.ImportProfile(openFileDialog.FileName, out var errorMessage);
        RefreshStatus(
            success ? ShellIntegrationText.StatusProfileImportCompleted : $"{ShellIntegrationText.StatusProfileImportFailed} {errorMessage}".Trim(),
            !success);
    }

    private void ApplyDefaultPresetButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyPreset(ShellIntegrationPreset.Default, ShellIntegrationText.StatusPresetAppliedDefault);
    }

    private void ApplyCompactDarkPresetButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyPreset(ShellIntegrationPreset.CompactDark, ShellIntegrationText.StatusPresetAppliedCompactDark);
    }

    private void ApplyMinimalLightPresetButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyPreset(ShellIntegrationPreset.MinimalLight, ShellIntegrationText.StatusPresetAppliedMinimalLight);
    }

    private void ApplyPreset(ShellIntegrationPreset preset, string successMessage)
    {
        var success = _plugin.ApplyPreset(preset);
        RefreshStatus(success ? successMessage : ShellIntegrationText.StatusPresetApplyFailed, !success);
    }
}
