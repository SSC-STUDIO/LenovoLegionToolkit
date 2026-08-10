using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Windows.Dashboard;
using Button = UniversalDeviceToolkit.Avalonia.Controls.Button;
using CardExpander = UniversalDeviceToolkit.Avalonia.Controls.Custom.CardExpander;
using CardHeaderControl = UniversalDeviceToolkit.Avalonia.Controls.CardHeaderControl;
using SymbolIcon = UniversalDeviceToolkit.Avalonia.Controls.SymbolIcon;
using SymbolRegular = UniversalDeviceToolkit.Avalonia.Controls.SymbolRegular;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard.Edit;

public class EditDashboardGroupControl : UserControl
{
    private readonly CardExpander _cardExpander = new()
    {
        Margin = new(0, 0, 0, 8)
    };

    private readonly CardHeaderControl _cardHeaderControl = new();

    private readonly StackPanel _stackPanel = new();

    private readonly StackPanel _itemsStackPanel = new();

    private readonly StackPanel _buttonsStackPanel = new()
    {
        Margin = new(0, 0, 16, 0),
        Orientation = Orientation.Horizontal
    };

    private readonly Button _editButton = new()
    {
        Icon = new SymbolIcon { Symbol = SymbolRegular.Edit24 },
        MinWidth = 34,
        Height = 34,
        Margin = new(8, 0, 0, 0),
    };

    private readonly Button _moveUpButton = new()
    {
        Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowUp24 },
        MinWidth = 34,
        Height = 34,
        Margin = new(8, 0, 0, 0),
    };

    private readonly Button _moveDownButton = new()
    {
        Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowDown24 },
        MinWidth = 34,
        Height = 34,
        Margin = new(8, 0, 0, 0),
    };

    private readonly Button _deleteButton = new()
    {
        Icon = new SymbolIcon { Symbol = SymbolRegular.Dismiss24 },
        MinWidth = 34,
        Height = 34,
        Margin = new(8, 0, 0, 0),
    };

    private readonly Button _addItemButton = new()
    {
        MinWidth = 120,
        HorizontalAlignment = HorizontalAlignment.Right,
        Appearance = UniversalDeviceToolkit.Avalonia.Controls.ControlAppearance.Primary,
        Content = Resource.Add,
        Margin = new(0, 8, 0, 0),
    };

    public event EventHandler? MoveUp;
    public event EventHandler? MoveDown;
    public event EventHandler? Delete;
    public event EventHandler? Changed;

    private DashboardGroupType _dashboardGroupType;
    private readonly Func<IEnumerable<DashboardItem>> _getExistingItems;

    public EditDashboardGroupControl(DashboardGroup dashboardGroup, Func<IEnumerable<DashboardItem>> getExistingItems)
    {
        _dashboardGroupType = dashboardGroup.Type;

        _getExistingItems = getExistingItems;

        ToolTip.SetTip(_editButton, Resource.Edit);
        ToolTip.SetTip(_moveUpButton, Resource.MoveUp);
        ToolTip.SetTip(_moveDownButton, Resource.MoveDown);
        ToolTip.SetTip(_deleteButton, Resource.Delete);

        _editButton.Click += async (_, _) => await EditNameAsync();
        _moveUpButton.Click += (_, _) => MoveUp?.Invoke(this, EventArgs.Empty);
        _moveDownButton.Click += (_, _) => MoveDown?.Invoke(this, EventArgs.Empty);
        _deleteButton.Click += (_, _) => Delete?.Invoke(this, EventArgs.Empty);
        _addItemButton.Click += (_, _) => ShowAddItemWindow();

        _buttonsStackPanel.Children.Add(_editButton);
        _buttonsStackPanel.Children.Add(_moveUpButton);
        _buttonsStackPanel.Children.Add(_moveDownButton);
        _buttonsStackPanel.Children.Add(_deleteButton);

        foreach (var item in dashboardGroup.Items)
            _itemsStackPanel.Children.Add(CreateGroupControl(item));

        _stackPanel.Children.Add(_itemsStackPanel);
        _stackPanel.Children.Add(_addItemButton);

        _cardHeaderControl.Title = dashboardGroup.GetName();
        _cardHeaderControl.Accessory = _buttonsStackPanel;
        _cardExpander.Header = _cardHeaderControl;
        _cardExpander.Content = _stackPanel;

        AutomationProperties.SetName(_cardExpander, _cardHeaderControl.Title);
        AutomationProperties.SetName(_editButton, _cardHeaderControl.Title);
        AutomationProperties.SetName(_moveUpButton, _cardHeaderControl.Title);
        AutomationProperties.SetName(_moveDownButton, _cardHeaderControl.Title);
        AutomationProperties.SetName(_deleteButton, _cardHeaderControl.Title);

        Content = _cardExpander;
    }

    public DashboardGroup GetDashboardGroup()
    {
        var items = _itemsStackPanel.Children
            .OfType<EditDashboardItemControl>()
            .Select(c => c.DashboardItem)
            .ToArray();
        return new(_dashboardGroupType, _cardHeaderControl.Title, items);
    }

    public IEnumerable<DashboardItem> GetItems() =>
        _itemsStackPanel.Children
            .OfType<EditDashboardItemControl>()
            .Select(c => c.DashboardItem);

    private async Task EditNameAsync()
    {
        var text = _dashboardGroupType == DashboardGroupType.Custom ? _cardHeaderControl.Title : null;

        var result = await MessageBoxHelper.ShowInputAsync(this,
            Resource.EditDashboardGroupControl_EditGroup_Title,
            Resource.EditDashboardGroupControl_EditGroup_Message,
            text,
            primaryButton: Resource.OK,
            secondaryButton: Resource.Cancel);

        if (string.IsNullOrEmpty(result))
            return;

        _dashboardGroupType = DashboardGroupType.Custom;
        _cardHeaderControl.Title = result;
    }

    private void ShowAddItemWindow()
    {
        var owner = TopLevel.GetTopLevel(this) as Window ?? UdtAppContext.MainWindow;
        var window = new AddDashboardItemWindow(_getExistingItems, AddItem);
        window.ShowDialog(owner);
    }

    private void AddItem(DashboardItem dashboardItem)
    {
        _itemsStackPanel.Children.Add(CreateGroupControl(dashboardItem));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private EditDashboardItemControl CreateGroupControl(DashboardItem dashboardItem)
    {
        var control = new EditDashboardItemControl(dashboardItem);
        control.MoveUp += (_, _) => MoveItemUp(control);
        control.MoveDown += (_, _) => MoveItemDown(control);
        control.Delete += (_, _) => DeleteItem(control);
        return control;
    }

    private void MoveItemUp(Control control)
    {
        var index = _itemsStackPanel.Children.IndexOf(control);
        index--;

        if (index < 0)
            return;

        _itemsStackPanel.Children.Remove(control);
        _itemsStackPanel.Children.Insert(index, control);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void MoveItemDown(Control control)
    {
        var index = _itemsStackPanel.Children.IndexOf(control);
        index++;

        if (index >= _itemsStackPanel.Children.Count)
            return;

        _itemsStackPanel.Children.Remove(control);
        _itemsStackPanel.Children.Insert(index, control);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void DeleteItem(Control control)
    {
        _itemsStackPanel.Children.Remove(control);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void RefreshAdd() => _addItemButton.IsEnabled = Enum.GetValues<DashboardItem>().Except(_getExistingItems()).Any();
}
