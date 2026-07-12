using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Network;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;

namespace UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;

public partial class NetworkAccelerationControl : UserControl
{
    private readonly INetworkAccelerationService _acceleration;
    private readonly INetworkDiagnosticsService _diagnostics;
    private readonly INetworkStateRecoveryService _recovery;
    private bool _suppressEvents;

    private static string T(string key, string fallback) =>
        LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    public NetworkAccelerationControl()
    {
        _acceleration = IoCContainer.Resolve<INetworkAccelerationService>();
        _diagnostics = IoCContainer.Resolve<INetworkDiagnosticsService>();
        _recovery = IoCContainer.Resolve<INetworkStateRecoveryService>();
        InitializeComponent();
        Loaded += NetworkAccelerationControl_Loaded;
        Unloaded += NetworkAccelerationControl_Unloaded;
    }

    private void NetworkAccelerationControl_Loaded(object sender, RoutedEventArgs e)
    {
        BuildModeCombo();
        BuildDomainGroupChecks();
        RefreshUi();
    }

    private async void NetworkAccelerationControl_Unloaded(object sender, RoutedEventArgs e)
    {
        // Keep acceleration running when leaving the page; stop is explicit only.
        await System.Threading.Tasks.Task.CompletedTask;
    }

    private void BuildModeCombo()
    {
        if (_modeComboBox is null)
            return;

        _suppressEvents = true;
        try
        {
            _modeComboBox.Items.Clear();
            _modeComboBox.Items.Add(new ComboBoxItem
            {
                Content = T("NetworkAccelerationPage_Mode_SystemProxy", "System proxy (PAC / local proxy)"),
                Tag = NetworkAccelerationMode.SystemProxy
            });
            _modeComboBox.Items.Add(new ComboBoxItem
            {
                Content = T("NetworkAccelerationPage_Mode_Hosts", "Hosts rewrite (UDT-marked block)"),
                Tag = NetworkAccelerationMode.Hosts
            });
            _modeComboBox.Items.Add(new ComboBoxItem
            {
                Content = T("NetworkAccelerationPage_Mode_DiagnosticsOnly", "Diagnostics only (no system changes)"),
                Tag = NetworkAccelerationMode.DiagnosticsOnly
            });
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private void BuildDomainGroupChecks()
    {
        if (_domainGroupsPanel is null)
            return;

        _domainGroupsPanel.Children.Clear();
        var groups = _acceleration.Config.DomainGroups;
        if (groups is null || groups.Count == 0)
        {
            _acceleration.Config.DomainGroups = BuiltinDomainGroups.CreateDefaults();
            groups = _acceleration.Config.DomainGroups;
        }

        foreach (var group in groups)
        {
            var check = new CheckBox
            {
                Content = $"{group.DisplayName} ({group.Domains?.Count ?? 0})",
                IsChecked = group.Enabled,
                Margin = new Thickness(0, 0, 0, 6),
                Tag = group.Id,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            check.Checked += DomainGroupCheck_Changed;
            check.Unchecked += DomainGroupCheck_Changed;
            AutomationProperties.SetAutomationId(check, $"NetworkAccelerationDomain_{group.Id}");
            _domainGroupsPanel.Children.Add(check);
        }
    }

    private void RefreshUi()
    {
        _suppressEvents = true;
        try
        {
            var config = _acceleration.Config;
            _statusText.Text = _acceleration.StatusText;
            _modeText.Text = $"{Resource.NetworkAccelerationPage_ModeLabel}: {config.Mode}";
            _portText.Text = $"{Resource.NetworkAccelerationPage_PortLabel}: {config.ListenPort}";

            if (_enableToggle is not null)
                _enableToggle.IsChecked = config.AccelerationEnabled;

            // Mode combo (Off is represented by master switch off).
            if (_modeComboBox is not null)
            {
                var displayMode = config.Mode is NetworkAccelerationMode.Off
                    ? NetworkAccelerationMode.SystemProxy
                    : config.Mode;
                for (var i = 0; i < _modeComboBox.Items.Count; i++)
                {
                    if (_modeComboBox.Items[i] is ComboBoxItem item &&
                        item.Tag is NetworkAccelerationMode m &&
                        m == displayMode)
                    {
                        _modeComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }

            // Domain checks
            if (_domainGroupsPanel is not null)
            {
                foreach (var child in _domainGroupsPanel.Children.OfType<CheckBox>())
                {
                    var id = child.Tag as string;
                    var group = config.DomainGroups?.FirstOrDefault(g =>
                        string.Equals(g.Id, id, StringComparison.OrdinalIgnoreCase));
                    child.IsChecked = group?.Enabled == true;
                }
            }

            var groups = config.DomainGroups ?? [];
            var enabledCount = groups.Count(g => g.Enabled);
            var domainCount = groups.Where(g => g.Enabled).SelectMany(g => g.Domains ?? []).Count();
            _domainGroupsText.Text = string.Format(
                Resource.NetworkAccelerationPage_DomainGroupsSummary,
                Resource.NetworkAccelerationPage_DomainGroupsLabel,
                enabledCount,
                groups.Count,
                domainCount);

            var backendOk = _acceleration.IsBackendReady;
            var enabled = config.AccelerationEnabled && config.Mode is not NetworkAccelerationMode.Off;

            _startButton.IsEnabled = backendOk && enabled && !_acceleration.IsRunning
                && config.Mode is not NetworkAccelerationMode.DiagnosticsOnly;
            _stopButton.IsEnabled = _acceleration.IsRunning
                || (enabled && config.Mode is NetworkAccelerationMode.SystemProxy or NetworkAccelerationMode.Hosts);

            if (_restoreButton is not null)
                _restoreButton.IsEnabled = true;

            if (_modeComboBox is not null)
                _modeComboBox.IsEnabled = config.AccelerationEnabled;
            if (_domainGroupsPanel is not null)
                _domainGroupsPanel.IsEnabled = config.AccelerationEnabled;
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private async void EnableToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents || _enableToggle is null)
            return;

        try
        {
            var on = _enableToggle.IsChecked == true;
            _acceleration.Config.AccelerationEnabled = on;
            if (on && _acceleration.Config.Mode is NetworkAccelerationMode.Off)
                _acceleration.Config.Mode = NetworkAccelerationMode.SystemProxy;
            if (!on)
            {
                _acceleration.Config.Mode = NetworkAccelerationMode.Off;
                await _acceleration.StopAsync().ConfigureAwait(true);
            }

            await _acceleration.SaveConfigAsync().ConfigureAwait(true);
        }
        catch
        {
            // keep UI alive
        }

        RefreshUi();
    }

    private async void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents || _modeComboBox?.SelectedItem is not ComboBoxItem { Tag: NetworkAccelerationMode mode })
            return;

        try
        {
            if (!_acceleration.Config.AccelerationEnabled)
                return;

            _acceleration.Config.Mode = mode;
            await _acceleration.SaveConfigAsync().ConfigureAwait(true);

            // Mode change while running: stop so user re-starts cleanly under new mode.
            if (_acceleration.IsRunning)
                await _acceleration.StopAsync().ConfigureAwait(true);
        }
        catch
        {
            // ignore
        }

        RefreshUi();
    }

    private async void DomainGroupCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents || sender is not CheckBox { Tag: string id } check)
            return;

        try
        {
            var group = _acceleration.Config.DomainGroups?.FirstOrDefault(g =>
                string.Equals(g.Id, id, StringComparison.OrdinalIgnoreCase));
            if (group is null)
                return;

            group.Enabled = check.IsChecked == true;
            await _acceleration.SaveConfigAsync().ConfigureAwait(true);
        }
        catch
        {
            // ignore
        }

        RefreshUi();
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_acceleration.Config.AccelerationEnabled)
            {
                _acceleration.Config.AccelerationEnabled = true;
                if (_acceleration.Config.Mode is NetworkAccelerationMode.Off)
                    _acceleration.Config.Mode = NetworkAccelerationMode.SystemProxy;
                await _acceleration.SaveConfigAsync().ConfigureAwait(true);
            }

            var ok = await _acceleration.StartAsync().ConfigureAwait(true);
            if (!ok)
                _diagnosticsText.Text = Resource.NetworkAccelerationPage_StartFailed;
        }
        catch (Exception ex)
        {
            _diagnosticsText.Text = $"{Resource.NetworkAccelerationPage_DiagnosticsFailed}: {ex.Message}";
        }

        RefreshUi();
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _acceleration.StopAsync().ConfigureAwait(true);
        }
        catch (Exception)
        {
            // ignore
        }

        RefreshUi();
    }

    private async void DiagnosticsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var report = await _diagnostics.RunQuickCheckAsync().ConfigureAwait(true);
            _diagnosticsText.Text = report.Summary;
        }
        catch (Exception ex)
        {
            _diagnosticsText.Text = $"{Resource.NetworkAccelerationPage_DiagnosticsFailed}: {ex.Message}";
        }
    }

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _ = _acceleration.StopAsync();
            var ok = _recovery.TryRestoreFromSnapshot(out var report);
            _diagnosticsText.Text = ok
                ? report
                : $"{Resource.NetworkAccelerationPage_RestorePartial}\n{report}";
        }
        catch (Exception ex)
        {
            _diagnosticsText.Text = $"{Resource.NetworkAccelerationPage_DiagnosticsFailed}: {ex.Message}";
        }

        RefreshUi();
    }
}
