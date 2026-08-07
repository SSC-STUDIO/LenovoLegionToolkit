using Avalonia.Controls;
using Avalonia.Interactivity;
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
        ArrangeSensorSurfaceLikeWpf();
        _platformServices = platformServices;
        _viewModel = new DashboardPageViewModel(
            platformServices,
            navigate: navigate,
            showHybridInfo: ShowHybridModeInfo,
            showPowerModeSettings: ShowPowerModeSettings);
        DataContext = _viewModel;
        AttachedToVisualTree += (_, _) => _viewModel.StartPolling();
        DetachedFromVisualTree += (_, _) => _viewModel.StopPolling();
    }

    private void ArrangeSensorSurfaceLikeWpf()
    {
        // WPF owns the sensor surface directly below the page header. The
        // Avalonia page also has a settings/layout editor, so move the same
        // controls to the top of the existing stack instead of duplicating
        // telemetry state or introducing a second dashboard composition.
        if (!DashboardStack.Children.Remove(TelemetryCardsPanel))
            return;

        DashboardStack.Children.Remove(SensorsHeaderPanel);
        var insertIndex = Math.Min(1, DashboardStack.Children.Count);
        DashboardStack.Children.Insert(insertIndex, TelemetryCardsPanel);
        DashboardStack.Children.Insert(Math.Min(insertIndex + 1, DashboardStack.Children.Count), SensorsHeaderPanel);
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

    private async void ShowPowerModeSettings(string state)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner || _viewModel is null)
            return;

        Window dialog = state.Equals("Balance", StringComparison.OrdinalIgnoreCase)
            ? new BalanceModeSettingsWindow(_platformServices)
            : new GodModeSettingsWindow(_platformServices);
        await dialog.ShowDialog(owner);
        await _viewModel.LoadAsync();
    }

    private async void ConfigureGpuOverclockButton_Click(object? sender, RoutedEventArgs e)
    {
#if WINDOWS
        if (TopLevel.GetTopLevel(this) is not Window owner || _viewModel is null)
            return;

        await new GpuOverclockProfilesWindow().ShowDialog(owner);
        await _viewModel.LoadAsync();
#endif
    }
}
