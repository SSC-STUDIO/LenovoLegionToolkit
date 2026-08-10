using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard;

public partial class SensorsControl
{
    private void HardwareSensorSettings_SectionsChanged(object? sender, EventArgs e)
    {
        void ApplyConfiguration()
        {
            ApplySensorSectionConfiguration();
            ApplySensorSummaryLayout(Bounds.Width > 1 ? Bounds.Width : 1200, force: true);
        }

        if (Dispatcher.UIThread.CheckAccess())
            ApplyConfiguration();
        else
            _ = Dispatcher.UIThread.InvokeAsync(ApplyConfiguration);
    }

    private void ApplySensorSectionConfiguration()
    {
        var store = _hardwareSensorSettings.Store;
        var visible = new HashSet<string>(store.VisibleSections ?? [], StringComparer.OrdinalIgnoreCase);
        var sectionMap = new Dictionary<string, Control>(StringComparer.OrdinalIgnoreCase)
        {
            ["CPU"] = _cpuSection,
            ["Battery"] = _batterySectionColumn,
            ["GPU"] = _gpuSection
        };
        var skeletonSectionMap = new Dictionary<string, Control>(StringComparer.OrdinalIgnoreCase)
        {
            ["CPU"] = _skeletonCpuSection,
            ["Battery"] = _skeletonBatterySection,
            ["GPU"] = _skeletonGpuSection
        };

        foreach (var (name, element) in sectionMap)
        {
            element.IsVisible = visible.Contains(name) ? true : false;
            skeletonSectionMap[name].IsVisible = element.IsVisible;
        }

        var order = (store.SectionOrder is { Length: > 0 } ? store.SectionOrder : ["CPU", "Battery", "GPU"])
            .Where(name => sectionMap.ContainsKey(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var orderedVisible = new List<Control>();
        foreach (var name in order)
        {
            if (sectionMap.TryGetValue(name, out var element) && element.IsVisible)
                orderedVisible.Add(element);
        }

        foreach (var (name, element) in sectionMap)
        {
            if (element.IsVisible && !orderedVisible.Contains(element))
                orderedVisible.Add(element);
        }

        _sensorsGrid.Children.Clear();
        for (var i = 0; i < orderedVisible.Count; i++)
        {
            Grid.SetColumn(orderedVisible[i], i);
            _sensorsGrid.Children.Add(orderedVisible[i]);
        }

        _skeletonGrid.Children.Clear();
        foreach (var name in order)
        {
            if (skeletonSectionMap.TryGetValue(name, out var element) && element.IsVisible)
                _skeletonGrid.Children.Add(element);
        }
        foreach (var (name, element) in skeletonSectionMap)
        {
            if (element.IsVisible && !_skeletonGrid.Children.Contains(element))
                _skeletonGrid.Children.Add(element);
        }

        var columnCount = Math.Max(1, orderedVisible.Count);
        SetColumnCount(_sensorsGrid, columnCount);
        SetColumnCount(_skeletonGrid, columnCount);
    }

    // AVALONIA: WPF UniformGrid.Columns -> Grid star columns (equal-width cells like UniformGrid).
    private static void SetColumnCount(Grid grid, int count)
    {
        grid.ColumnDefinitions.Clear();
        for (var i = 0; i < count; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
    }
}
