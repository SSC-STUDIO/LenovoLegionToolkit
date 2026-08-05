using Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class DashboardPage : UserControl
{
    private DashboardPageViewModel? _viewModel;

    public DashboardPage(IPlatformServices platformServices, Action<string>? navigate = null)
    {
        InitializeComponent();
        _viewModel = new DashboardPageViewModel(platformServices, navigate: navigate);
        DataContext = _viewModel;
        AttachedToVisualTree += (_, _) => _viewModel.StartPolling();
        DetachedFromVisualTree += (_, _) => _viewModel.StopPolling();
    }
}
