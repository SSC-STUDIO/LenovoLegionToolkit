using Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class DashboardPage : UserControl
{
    private DashboardPageViewModel? _viewModel;

    public DashboardPage(IPlatformServices platformServices, Action<string>? navigate = null)
    {
        InitializeComponent();
        _viewModel = new DashboardPageViewModel(
            platformServices,
            navigate: navigate,
            showHybridInfo: ShowHybridModeInfo);
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
}
