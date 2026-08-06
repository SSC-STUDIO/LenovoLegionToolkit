using Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Pages.Windows;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class DashboardPage : UserControl
{
    private DashboardPageViewModel? _viewModel;
    private readonly IPlatformServices _platformServices;

    public DashboardPage(IPlatformServices platformServices, Action<string>? navigate = null)
    {
        InitializeComponent();
        _platformServices = platformServices;
        _viewModel = new DashboardPageViewModel(
            platformServices,
            navigate: navigate,
            showHybridInfo: ShowHybridModeInfo,
            showBalanceSettings: ShowBalanceModeSettings);
        DataContext = _viewModel;
        AttachedToVisualTree += (_, _) => _viewModel.StartPolling();
        DetachedFromVisualTree += (_, _) => _viewModel.StopPolling();
    }

    private async void ShowHybridModeInfo(IReadOnlyList<DashboardStateOption> options)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
        {
            return;
        }

        var dialog = new HybridModeInfoWindow(options);
        await dialog.ShowDialog(owner);
    }

    private async void ShowBalanceModeSettings()
    {
        if (TopLevel.GetTopLevel(this) is not Window owner || _viewModel is null)
            return;

        var dialog = new BalanceModeSettingsWindow(_platformServices);
        await dialog.ShowDialog(owner);
        await _viewModel.LoadAsync();
    }
}
