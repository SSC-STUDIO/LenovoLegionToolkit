using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using UniversalDeviceToolkit.WPF.Settings;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Windows.Settings;

public partial class HardwareSensorSectionsWindow
{
    private static readonly string[] AllSections = ["CPU", "Battery", "GPU"];
    private readonly HardwareSensorSettings _settings = IoCContainer.Resolve<HardwareSensorSettings>();
    private readonly ListBox _orderList = new() { Height = 140 };
    private readonly Dictionary<string, ToggleSwitch> _visibilityToggles = new(StringComparer.OrdinalIgnoreCase);

    public HardwareSensorSectionsWindow()
    {
        InitializeComponent();
        _orderList.ItemContainerStyle = (Style)FindResource("SensorSectionListItemStyle");

        var store = _settings.Store;
        var order = (store.SectionOrder is { Length: > 0 } ? store.SectionOrder : AllSections)
            .Where(section => AllSections.Contains(section, StringComparer.OrdinalIgnoreCase))
            .Concat(AllSections.Where(section => !(store.SectionOrder ?? []).Contains(section, StringComparer.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var visible = new HashSet<string>(store.VisibleSections ?? AllSections, StringComparer.OrdinalIgnoreCase);

        foreach (var section in AllSections)
        {
            var toggle = new ToggleSwitch
            {
                Content = section,
                IsChecked = visible.Contains(section),
                Margin = new Thickness(0, 0, 0, 8)
            };
            _visibilityToggles[section] = toggle;
            _visibilityPanel.Children.Add(toggle);
        }

        foreach (var section in order)
            _orderList.Items.Add(section);

        _orderHost.Children.Add(_orderList);
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        var index = _orderList.SelectedIndex;
        if (index <= 0)
            return;

        var item = _orderList.Items[index];
        _orderList.Items.RemoveAt(index);
        _orderList.Items.Insert(index - 1, item);
        _orderList.SelectedIndex = index - 1;
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        var index = _orderList.SelectedIndex;
        if (index < 0 || index >= _orderList.Items.Count - 1)
            return;

        var item = _orderList.Items[index];
        _orderList.Items.RemoveAt(index);
        _orderList.Items.Insert(index + 1, item);
        _orderList.SelectedIndex = index + 1;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _settings.Store.VisibleSections = _visibilityToggles
            .Where(pair => pair.Value.IsChecked == true)
            .Select(pair => pair.Key)
            .ToArray();

        if (_settings.Store.VisibleSections.Length == 0)
            _settings.Store.VisibleSections = AllSections.ToArray();

        _settings.Store.SectionOrder = _orderList.Items.Cast<object>().Select(item => item.ToString()!).ToArray();
        _settings.SynchronizeStore();
        _settings.NotifySectionsChanged();
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
