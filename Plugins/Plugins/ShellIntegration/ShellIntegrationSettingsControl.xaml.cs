using System;
using System.IO;
using System.Windows;
#nullable enable

using System.Windows.Controls;
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

        var root = new Grid { Margin = new Thickness(16) };
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
        _enableButton.Click += EnableButton_Click;
        _disableButton = new Wpf.Ui.Controls.Button { Content = ShellIntegrationText.DisableButton, Width = 90, Margin = new Thickness(8, 0, 0, 0) };
        _disableButton.Click += DisableButton_Click;
        _openStyleSettingsButton = new Wpf.Ui.Controls.Button { Content = ShellIntegrationText.OpenStyleShortButton, Width = 120, Margin = new Thickness(8, 0, 0, 0) };
        _openStyleSettingsButton.Click += OpenStyleButton_Click;
        _openShellFolderButton = new Wpf.Ui.Controls.Button { Content = ShellIntegrationText.OpenShellFolderButton, Width = 160, Margin = new Thickness(8, 0, 0, 0) };
        _openShellFolderButton.Click += OpenShellFolderButton_Click;
        _openConfigButton = new Wpf.Ui.Controls.Button { Content = ShellIntegrationText.OpenConfigButton, Width = 140, Margin = new Thickness(8, 0, 0, 0) };
        _openConfigButton.Click += OpenConfigButton_Click;
        buttonPanel.Children.Add(_enableButton);
        buttonPanel.Children.Add(_disableButton);
        buttonPanel.Children.Add(_openStyleSettingsButton);
        buttonPanel.Children.Add(_openShellFolderButton);
        buttonPanel.Children.Add(_openConfigButton);

        Grid.SetRow(buttonPanel, 2);
        root.Children.Add(buttonPanel);

        Content = root;
    }

    private void RefreshStatus(string? suffix = null, bool? isError = null)
    {
        if (_statusTextBlock is null)
            return;

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
        var canOpenStyleSettings = canManageShell;

        if (_registrationValueTextBlock != null)
            _registrationValueTextBlock.Text = installed
                ? (isRegistered ? ShellIntegrationText.RegisteredState : ShellIntegrationText.MissingState)
                : ShellIntegrationText.MissingState;

        if (_versionValueTextBlock != null)
            _versionValueTextBlock.Text = version;

        if (_configValueTextBlock != null)
            _configValueTextBlock.Text = configPath;

        if (_pathValueTextBlock != null)
            _pathValueTextBlock.Text = path;

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

        if (_openStyleSettingsButton != null)
            _openStyleSettingsButton.IsEnabled = canOpenStyleSettings;

        if (_openShellFolderButton != null)
            _openShellFolderButton.IsEnabled = canOpenShellFolder;

        if (_openConfigButton != null)
            _openConfigButton.IsEnabled = canOpenConfig;

        _statusTextBlock.Text = $"{prefix}\n{ShellIntegrationText.PathLabel}: {path}";
        if (!string.IsNullOrWhiteSpace(version) && version != ShellIntegrationText.NotFound)
            _statusTextBlock.Text += $"\n{ShellIntegrationText.VersionLabel}: {version}";
        if (!allowSystemActions)
            _statusTextBlock.Text += "\nPreview mode: runtime actions are disabled.";
        if (!string.IsNullOrWhiteSpace(suffix))
            _statusTextBlock.Text += $"\n{suffix}";

        var effectiveIsError = isError ?? !installed || !isRegistered;
        _statusTextBlock.Foreground = effectiveIsError
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(196, 43, 28))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 123, 90));

        if (_statusIcon is not null)
        {
            _statusIcon.Symbol = effectiveIsError
                ? Wpf.Ui.Common.SymbolRegular.ErrorCircle24
                : Wpf.Ui.Common.SymbolRegular.CheckmarkCircle24;
            _statusIcon.Foreground = _statusTextBlock.Foreground;
        }
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
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"EnableButton_Click error: {ex.Message}", ex);
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
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"DisableButton_Click error: {ex.Message}", ex);
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
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"OpenStyleButton_Click error: {ex.Message}", ex);
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
}
