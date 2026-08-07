using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Avalonia.Pages.Windows;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

/// <summary>
/// Responsive column-count rule for the dashboard group cards. Mirrors the WPF
/// dashboard reflow (1/2/3 columns) with the Avalonia breakpoints: <700 one column,
/// 700-1100 two columns, >1100 three columns.
/// </summary>
internal static class DashboardColumnLayout
{
    public const double TwoColumnBreakpoint = 700.0;
    public const double ThreeColumnBreakpoint = 1100.0;

    public static int GetColumnCountForWidth(double width)
    {
        if (width > ThreeColumnBreakpoint)
            return 3;
        if (width >= TwoColumnBreakpoint)
            return 2;
        return 1;
    }
}

/// <summary>
/// Wrap panel that sizes each dashboard group card to the container width divided by
/// the responsive column count, preserving the WPF group reflow behavior on resize.
/// </summary>
internal sealed class DashboardWrapPanel : Panel
{
    public double HorizontalSpacing { get; set; } = 12;
    public double VerticalSpacing { get; set; } = 12;

    private double _measuredHeight;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (double.IsInfinity(availableSize.Width)
            || double.IsNaN(availableSize.Width)
            || availableSize.Width <= 0)
        {
            var desired = new Size();
            foreach (var child in Children)
            {
                child.Measure(availableSize);
                desired = new Size(
                    Math.Max(desired.Width, child.DesiredSize.Width),
                    Math.Max(desired.Height, child.DesiredSize.Height));
            }

            return desired;
        }

        var columns = DashboardColumnLayout.GetColumnCountForWidth(availableSize.Width);
        var usedColumns = Math.Min(columns, Math.Max(1, Children.Count));
        var spacing = HorizontalSpacing * Math.Max(0, usedColumns - 1);
        var childWidth = Math.Max(1.0, (availableSize.Width - spacing) / usedColumns);
        var rowHeight = 0.0;
        var totalHeight = 0.0;
        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            child.Measure(new Size(childWidth, double.PositiveInfinity));
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            if ((index + 1) % usedColumns == 0 || index == Children.Count - 1)
            {
                totalHeight += rowHeight;
                if (index < Children.Count - 1)
                    totalHeight += VerticalSpacing;
                rowHeight = 0.0;
            }
        }

        _measuredHeight = totalHeight;
        return new Size(availableSize.Width, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns = DashboardColumnLayout.GetColumnCountForWidth(finalSize.Width);
        var usedColumns = Math.Min(columns, Math.Max(1, Children.Count));
        var spacing = HorizontalSpacing * Math.Max(0, usedColumns - 1);
        var childWidth = Math.Max(1.0, (finalSize.Width - spacing) / usedColumns);
        var x = 0.0;
        var y = 0.0;
        var rowHeight = 0.0;
        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            var width = childWidth;
            if (usedColumns > 1 && index == Children.Count - 1 && index % usedColumns == 0)
                width = Math.Max(childWidth, finalSize.Width - x);
            child.Arrange(new Rect(x, y, width, child.DesiredSize.Height));
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            x += width + HorizontalSpacing;
            if ((index + 1) % usedColumns == 0)
            {
                y += rowHeight + VerticalSpacing;
                x = 0.0;
                rowHeight = 0.0;
            }
        }

        return new Size(finalSize.Width, _measuredHeight);
    }
}

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
