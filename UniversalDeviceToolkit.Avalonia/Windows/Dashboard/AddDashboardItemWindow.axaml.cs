using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Extensions;

namespace UniversalDeviceToolkit.Avalonia.Windows.Dashboard
{
public partial class AddDashboardItemWindow : global::UniversalDeviceToolkit.Avalonia.Windows.BaseWindow
{
    private readonly Func<IEnumerable<DashboardItem>> _existingItems;
    private readonly Action<DashboardItem> _addDashboardItem;

    public AddDashboardItemWindow(Func<IEnumerable<DashboardItem>> existingItems, Action<DashboardItem> addDashboardItem)
    {
        _existingItems = existingItems;
        _addDashboardItem = addDashboardItem;

        InitializeComponent();

        PropertyChanged += AddDashboardItemWindow_PropertyChanged;
    }

    private async void AddDashboardItemWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Visual.IsVisibleProperty)
            return;

        try
        {
            if (IsVisible)
                await RefreshAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(AddDashboardItemWindow_PropertyChanged)}.", ex);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

    private Task RefreshAsync()
    {
        _content.Children.Clear();

        var allItems = Enum.GetValues<DashboardItem>();
        var existingItems = _existingItems().ToArray();

        foreach (var item in allItems.Except(existingItems))
            _content.Children.Add(CreateCardControl(item));

        return Task.CompletedTask;
    }

    private CardControl CreateCardControl(DashboardItem item)
    {
        var control = new CardControl
        {
            Icon = new SymbolIcon { Symbol = item.GetIcon() },
            Header = new CardHeaderControl
            {
                Title = item.GetTitle(),
                Accessory = new SymbolIcon { Symbol = SymbolRegular.ChevronRight24 }
            },
            Margin = new(0, 8, 0, 0),
        };

        control.Click += (_, _) =>
        {
            _addDashboardItem(item);
            Close();
        };

        return control;
    }
}
}
