using System;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Network;
using UniversalDeviceToolkit.WPF.Resources;

namespace UniversalDeviceToolkit.WPF.Pages;

public partial class NetworkAccelerationPage
{
    private readonly INetworkAccelerationService _acceleration;
    private readonly INetworkDiagnosticsService _diagnostics;

    public NetworkAccelerationPage()
    {
        _acceleration = IoCContainer.Resolve<INetworkAccelerationService>();
        _diagnostics = IoCContainer.Resolve<INetworkDiagnosticsService>();
        InitializeComponent();
        Loaded += NetworkAccelerationPage_Loaded;
    }

    private void NetworkAccelerationPage_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshUi();
    }

    private void RefreshUi()
    {
        var config = _acceleration.Config;
        _statusText.Text = _acceleration.StatusText;
        _modeText.Text = $"{Resource.NetworkAccelerationPage_ModeLabel}: {config.Mode}";
        _portText.Text = $"{Resource.NetworkAccelerationPage_PortLabel}: {config.ListenPort}";

        // Phase 1: controls stay disabled until the worker backend is ready.
        var canControl = _acceleration.IsBackendReady && config.AccelerationEnabled;
        _startButton.IsEnabled = canControl && !_acceleration.IsRunning;
        _stopButton.IsEnabled = canControl && _acceleration.IsRunning;
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _acceleration.StartAsync().ConfigureAwait(true);
        }
        catch (Exception)
        {
            // Proxy/worker failures must not crash the GUI.
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
}
