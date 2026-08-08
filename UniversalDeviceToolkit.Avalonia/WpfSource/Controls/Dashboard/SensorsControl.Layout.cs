using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard;

public partial class SensorsControl
{
    private void HardwareSensorSettings_SectionsChanged(object? sender, EventArgs e)
    {
        void ApplyConfiguration()
        {
            ApplySensorSectionConfiguration();
            ApplySensorSummaryLayout(ActualWidth > 1 ? ActualWidth : 1200, force: true);
        }

        if (Dispatcher.CheckAccess())
            ApplyConfiguration();
        else
            _ = Dispatcher.InvokeAsync(ApplyConfiguration);
    }

    private void ApplySensorSectionConfiguration()
    {
        var store = _hardwareSensorSettings.Store;
        var visible = new HashSet<string>(store.VisibleSections ?? [], StringComparer.OrdinalIgnoreCase);
        var sectionMap = new Dictionary<string, FrameworkElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["CPU"] = _cpuSection,
            ["Battery"] = _batterySectionColumn,
            ["GPU"] = _gpuSection
        };
        var skeletonSectionMap = new Dictionary<string, FrameworkElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["CPU"] = _skeletonCpuSection,
            ["Battery"] = _skeletonBatterySection,
            ["GPU"] = _skeletonGpuSection
        };

        foreach (var (name, element) in sectionMap)
        {
            element.Visibility = visible.Contains(name) ? Visibility.Visible : Visibility.Collapsed;
            skeletonSectionMap[name].Visibility = element.Visibility;
        }

        var order = (store.SectionOrder is { Length: > 0 } ? store.SectionOrder : ["CPU", "Battery", "GPU"])
            .Where(name => sectionMap.ContainsKey(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var orderedVisible = new List<UIElement>();
        foreach (var name in order)
        {
            if (sectionMap.TryGetValue(name, out var element) && element.Visibility == Visibility.Visible)
                orderedVisible.Add(element);
        }

        foreach (var (name, element) in sectionMap)
        {
            if (element.Visibility == Visibility.Visible && !orderedVisible.Contains(element))
                orderedVisible.Add(element);
        }

        _sensorsGrid.Children.Clear();
        foreach (var child in orderedVisible)
            _sensorsGrid.Children.Add(child);

        _skeletonGrid.Children.Clear();
        foreach (var name in order)
        {
            if (skeletonSectionMap.TryGetValue(name, out var element) && element.Visibility == Visibility.Visible)
                _skeletonGrid.Children.Add(element);
        }
        foreach (var (name, element) in skeletonSectionMap)
        {
            if (element.Visibility == Visibility.Visible && !_skeletonGrid.Children.Contains(element))
                _skeletonGrid.Children.Add(element);
        }

        var columnCount = Math.Max(1, orderedVisible.Count);
        _sensorsGrid.Columns = columnCount;
        _skeletonGrid.Columns = columnCount;
    }
}
