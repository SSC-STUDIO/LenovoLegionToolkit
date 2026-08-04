using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
#nullable enable

using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using UniversalDeviceToolkit.Plugins.Shared;

namespace UniversalDeviceToolkit.Plugins.ShellIntegration;

public partial class ShellIntegrationSettingsControl : UserControl
{
    private readonly ShellIntegrationPlugin _plugin;
    private bool _isShellActionBusy;

    public ShellIntegrationSettingsControl(ShellIntegrationPlugin plugin)
    {
        _plugin = plugin;
        WpfFallbackHelper.TryInitializeComponent(this, BuildFallbackUi);
        RefreshStatus();
    }

    /// <summary>
    /// Fallback-path button factory: MinWidth + Padding instead of fixed Width so
    /// localized text never clips (was "Apply Compact Darl…" truncated at fixed width).
    /// </summary>
    private static Wpf.Ui.Controls.Button MakeButton(object content, string automationId, RoutedEventHandler clickHandler, double minWidth)
    {
        var button = new Wpf.Ui.Controls.Button
        {
            Content = content,
            MinWidth = minWidth,
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 8, 8)
        };
        AutomationProperties.SetAutomationId(button, automationId);
        button.Click += clickHandler;
        return button;
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
        _enableButton = MakeButton(ShellIntegrationText.EnableButton, "EnableButton", EnableButton_Click, 90);
        _disableButton = MakeButton(ShellIntegrationText.DisableButton, "DisableButton", DisableButton_Click, 90);
        _openStyleSettingsButton = MakeButton(ShellIntegrationText.OpenStyleShortButton, "OpenStyleSettingsButton", OpenStyleButton_Click, 120);
        _openShellFolderButton = MakeButton(ShellIntegrationText.OpenShellFolderButton, "OpenShellFolderButton", OpenShellFolderButton_Click, 150);
        _openConfigButton = MakeButton(ShellIntegrationText.OpenConfigButton, "OpenConfigButton", OpenConfigButton_Click, 130);
        _syncManagedConfigButton = MakeButton(ShellIntegrationText.SyncManagedConfigButton, "SyncManagedConfigButton", SyncManagedConfigButton_Click, 150);
        _resetManagedConfigButton = MakeButton(ShellIntegrationText.ResetManagedConfigButton, "ResetManagedConfigButton", ResetManagedConfigButton_Click, 150);
        _openManagedConfigButton = MakeButton(ShellIntegrationText.OpenManagedConfigButton, "OpenManagedConfigButton", OpenManagedConfigButton_Click, 150);
        _exportProfileButton = MakeButton(ShellIntegrationText.ExportProfileButton, "ExportProfileButton", ExportProfileButton_Click, 130);
        _importProfileButton = MakeButton(ShellIntegrationText.ImportProfileButton, "ImportProfileButton", ImportProfileButton_Click, 130);
        _applyDefaultPresetButton = MakeButton(ShellIntegrationText.PresetDefaultButton, "ApplyDefaultPresetButton", ApplyDefaultPresetButton_Click, 130);
        _applyCompactDarkPresetButton = MakeButton(ShellIntegrationText.PresetCompactDarkButton, "ApplyCompactDarkPresetButton", ApplyCompactDarkPresetButton_Click, 150);
        _applyMinimalLightPresetButton = MakeButton(ShellIntegrationText.PresetMinimalLightButton, "ApplyMinimalLightPresetButton", ApplyMinimalLightPresetButton_Click, 150);
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
        _isShellActionBusy = false;
        if (_statusTextBlock is null)
        {
            return;
        }

        var allowSystemActions = UniversalDeviceToolkit.Plugins.SDK.PluginHostContextRuntime.Current.AllowSystemActions;
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
        var canOpenStyleSettings = canManageShell || UniversalDeviceToolkit.Plugins.SDK.PluginHostContextRuntime.Current.Mode == UniversalDeviceToolkit.Lib.Plugins.PluginHostMode.Preview;

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

    private void SetShellActionBusy(bool isBusy)
    {
        if (isBusy)
        {
            _isShellActionBusy = true;

            if (_enableButton is not null)
            {
                _enableButton.IsEnabled = false;
            }

            if (_disableButton is not null)
            {
                _disableButton.IsEnabled = false;
            }

            if (_statusTextBlock is not null)
            {
                _statusTextBlock.Text = ShellIntegrationText.StatusWorking;
                _statusTextBlock.Foreground = ResolveBrush("TextFillColorSecondaryBrush", SystemColors.ControlTextBrush);
            }

            return;
        }

        if (!_isShellActionBusy)
        {
            // RefreshStatus already ran and restored capability-based button state and text.
            return;
        }

        _isShellActionBusy = false;
        RefreshStatus();
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
        SetShellActionBusy(true);
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
        finally
        {
            SetShellActionBusy(false);
        }
    }

    private async void DisableButton_Click(object sender, RoutedEventArgs e)
    {
        SetShellActionBusy(true);
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
        finally
        {
            SetShellActionBusy(false);
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

    private async void SyncManagedConfigButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var success = await _plugin.SyncManagedConfigurationAsync().ConfigureAwait(true);
            RefreshStatus(success ? ShellIntegrationText.StatusManagedConfigSyncCompleted : ShellIntegrationText.StatusManagedConfigSyncFailed, !success);
        }
        catch (Exception ex)
        {
            RefreshStatus($"{ShellIntegrationText.ErrorPrefix}: {ex.Message}", true);
            PluginLog.Trace($"SyncManagedConfigButton_Click error: {ex.Message}", ex);
        }
    }

    private async void ResetManagedConfigButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var success = await _plugin.ResetManagedConfigurationAsync().ConfigureAwait(true);
            RefreshStatus(success ? ShellIntegrationText.StatusManagedConfigResetCompleted : ShellIntegrationText.StatusManagedConfigResetFailed, !success);
        }
        catch (Exception ex)
        {
            RefreshStatus($"{ShellIntegrationText.ErrorPrefix}: {ex.Message}", true);
            PluginLog.Trace($"ResetManagedConfigButton_Click error: {ex.Message}", ex);
        }
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

    private async void ImportProfileButton_Click(object sender, RoutedEventArgs e)
    {
        try
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

            var (success, errorMessage) = await _plugin.ImportProfileAsync(openFileDialog.FileName).ConfigureAwait(true);
            RefreshStatus(
                success ? ShellIntegrationText.StatusProfileImportCompleted : $"{ShellIntegrationText.StatusProfileImportFailed} {errorMessage}".Trim(),
                !success);
        }
        catch (Exception ex)
        {
            RefreshStatus($"{ShellIntegrationText.ErrorPrefix}: {ex.Message}", true);
            PluginLog.Trace($"ImportProfileButton_Click error: {ex.Message}", ex);
        }
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

    private async void ApplyPreset(ShellIntegrationPreset preset, string successMessage)
    {
        try
        {
            var success = await _plugin.ApplyPresetAsync(preset).ConfigureAwait(true);
            RefreshStatus(success ? successMessage : ShellIntegrationText.StatusPresetApplyFailed, !success);
        }
        catch (Exception ex)
        {
            RefreshStatus($"{ShellIntegrationText.ErrorPrefix}: {ex.Message}", true);
            PluginLog.Trace($"ApplyPreset error: {ex.Message}", ex);
        }
    }
}
