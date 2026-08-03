using Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class DashboardPage : UserControl
{
    public DashboardPage(IPlatformServices platformServices)
    {
        InitializeComponent();
        DataContext = new DashboardPageViewModel(platformServices);
        if (DataContext is DashboardPageViewModel viewModel)
        {
            _ = viewModel.LoadAsync();
        }
    }
}