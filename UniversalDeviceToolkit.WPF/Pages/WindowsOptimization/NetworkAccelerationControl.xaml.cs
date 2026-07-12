using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Network;
using UniversalDeviceToolkit.WPF.Resources;

namespace UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;

public partial class NetworkAccelerationControl : UserControl
{
    private readonly INetworkAccelerationService _acceleration;
    private readonly INetworkDiagnosticsService _diagnostics;
    private readonly INetworkStateRecoveryService _recovery;
    private bool _suppressEvents;

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
        RefreshUi();
    }

    private async void NetworkAccelerationControl_Unloaded(object sender, RoutedEventArgs e)
    {
        // Do not stop acceleration on page unload — user may leave the tab while proxy runs.
        // Stop is explicit only (Stop button / app exit / --reset-network-state).
        await System.Threading.Tasks.Task.CompletedTask;
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

            var groups = config.DomainGroups ?? [];
            var enabledCount = groups.Count(g => g.Enabled);
            var domainCount = groups.Where(g => g.Enabled).SelectMany(g => g.Domains ?? []).Count();
            _domainGroupsText.Text = string.Format(
                Resource.NetworkAccelerationPage_DomainGroupsSummary,
                Resource.NetworkAccelerationPage_DomainGroupsLabel,
                enabledCount,
                groups.Count,
                domainCount);

            // User may enable + start only when worker binary exists.
            // Diagnostics always allowed.
            var backendOk = _acceleration.IsBackendReady;
            var enabled = config.AccelerationEnabled && config.Mode is not NetworkAccelerationMode.Off;

            _startButton.IsEnabled = backendOk && enabled && !_acceleration.IsRunning
                && config.Mode is not NetworkAccelerationMode.DiagnosticsOnly;
            _stopButton.IsEnabled = _acceleration.IsRunning
                || (enabled && config.Mode is NetworkAccelerationMode.SystemProxy or NetworkAccelerationMode.Hosts);

            if (_restoreButton is not null)
                _restoreButton.IsEnabled = true;
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
            {
                _diagnosticsText.Text = Resource.NetworkAccelerationPage_StartFailed;
            }
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
