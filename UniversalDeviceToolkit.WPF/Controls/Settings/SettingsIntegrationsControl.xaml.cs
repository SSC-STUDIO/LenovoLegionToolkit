using System;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Integrations;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.System;
using UniversalDeviceToolkit.WPF.CLI;

namespace UniversalDeviceToolkit.WPF.Controls.Settings
{
public partial class SettingsIntegrationsControl
{
    private readonly IntegrationsSettings _integrationsSettings = IoCContainer.Resolve<IntegrationsSettings>();
    private readonly HWiNFOIntegration _hwinfoIntegration = IoCContainer.Resolve<HWiNFOIntegration>();
    private readonly IpcServer _ipcServer = IoCContainer.Resolve<IpcServer>();
    private bool _isRefreshing;

    public SettingsIntegrationsControl()
    {
        InitializeComponent();
    }

    public Task RefreshAsync()
    {
        _isRefreshing = true;

        _hwinfoIntegrationToggle.IsChecked = _integrationsSettings.Store.HWiNFO;
        _cliInterfaceToggle.IsChecked = _integrationsSettings.Store.CLI;
        _cliPathToggle.IsChecked = SystemPath.HasCLI();

        _isRefreshing = false;

        return Task.CompletedTask;
    }

    private async void HWiNFOIntegrationToggle_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isRefreshing)
                return;

            _integrationsSettings.Store.HWiNFO = _hwinfoIntegrationToggle.IsChecked ?? false;
            _integrationsSettings.SynchronizeStore();

            await _hwinfoIntegration.StartStopIfNeededAsync();
        }
        catch (Exception ex) { /* Logging excluded — no Log access in this scope */ }
    }

    private async void CLIInterfaceToggle_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isRefreshing)
                return;

            _integrationsSettings.Store.CLI = _cliInterfaceToggle.IsChecked ?? false;
            _integrationsSettings.SynchronizeStore();

            await _ipcServer.StartStopIfNeededAsync();
        }
        catch (Exception)
        {
            // Silently ignored — this method is a fire-and-forget click handler that
            // should never crash the UI even if the underlying operation fails.
        }
    }

    private void CLIPathToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        SystemPath.SetCLI(_cliPathToggle.IsChecked ?? false);
    }
}
}
