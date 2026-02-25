using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Settings;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

/// <summary>
/// Network Acceleration Settings Page - Advanced settings configuration
/// </summary>
public partial class NetworkAccelerationSettingsPage
{
    private readonly NetworkAccelerationSettings _settings = new();

    public NetworkAccelerationSettingsPage()
    {
        InitializeComponent();
        Loaded += NetworkAccelerationSettingsPage_Loaded;
    }

    private async void NetworkAccelerationSettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _settings.LoadAsync();
            
            // Load ConnectionTimeout
            if (_connectionTimeoutNumberBox != null)
            {
                _connectionTimeoutNumberBox.Value = _settings.ConnectionTimeout;
            }

            // Load ProxyAddress
            if (_proxyAddressTextBox != null)
            {
                _proxyAddressTextBox.Text = _settings.ProxyAddress ?? string.Empty;
            }

            // Load ProxyPort
            if (_proxyPortNumberBox != null)
            {
                _proxyPortNumberBox.Value = _settings.ProxyPort;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading network acceleration settings: {ex.Message}", ex);
        }
    }

    private void ConnectionTimeoutNumberBox_ValueChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not NumberBox numberBox)
            return;

        try
        {
            _settings.ConnectionTimeout = (int)(numberBox.Value ?? 30);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error updating connection timeout: {ex.Message}", ex);
        }
    }

    private void ProxyAddressTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.TextBox textBox)
            return;

        try
        {
            _settings.ProxyAddress = textBox.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error updating proxy address: {ex.Message}", ex);
        }
    }

    private void ProxyPortNumberBox_ValueChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not NumberBox numberBox)
            return;

        try
        {
            _settings.ProxyPort = (int)(numberBox.Value ?? 8888);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error updating proxy port: {ex.Message}", ex);
        }
    }
}

