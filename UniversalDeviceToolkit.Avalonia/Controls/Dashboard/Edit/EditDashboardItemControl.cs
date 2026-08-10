using System;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using Button = UniversalDeviceToolkit.Avalonia.Controls.Button;
using CardHeaderControl = UniversalDeviceToolkit.Avalonia.Controls.CardHeaderControl;
using SymbolIcon = UniversalDeviceToolkit.Avalonia.Controls.SymbolIcon;
using SymbolRegular = UniversalDeviceToolkit.Avalonia.Controls.SymbolRegular;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard.Edit;

public class EditDashboardItemControl : UserControl
{
    public DashboardItem DashboardItem { get; }

    private readonly CardControl _cardControl = new()
    {
        Margin = new(0, 0, 0, 8)
    };

    private readonly CardHeaderControl _cardHeaderControl = new();

    private readonly StackPanel _stackPanel = new()
    {
        Orientation = Orientation.Horizontal,
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

    public event EventHandler? MoveUp;
    public event EventHandler? MoveDown;
    public event EventHandler? Delete;

    public EditDashboardItemControl(DashboardItem dashboardItem)
    {
        DashboardItem = dashboardItem;

        ToolTip.SetTip(_moveUpButton, Resource.MoveUp);
        ToolTip.SetTip(_moveDownButton, Resource.MoveDown);
        ToolTip.SetTip(_deleteButton, Resource.Delete);

        _moveUpButton.Click += (_, _) => MoveUp?.Invoke(this, EventArgs.Empty);
        _moveDownButton.Click += (_, _) => MoveDown?.Invoke(this, EventArgs.Empty);
        _deleteButton.Click += (_, _) => Delete?.Invoke(this, EventArgs.Empty);

        _stackPanel.Children.Add(_moveUpButton);
        _stackPanel.Children.Add(_moveDownButton);
        _stackPanel.Children.Add(_deleteButton);

        _cardHeaderControl.Title = DashboardItem.GetTitle();
        _cardHeaderControl.Accessory = _stackPanel;
        _cardControl.Icon = new SymbolIcon { Symbol = DashboardItem.GetIcon() };
        _cardControl.Header = _cardHeaderControl;

        AutomationProperties.SetName(_moveUpButton, _cardHeaderControl.Title);
        AutomationProperties.SetName(_moveDownButton, _cardHeaderControl.Title);
        AutomationProperties.SetName(_deleteButton, _cardHeaderControl.Title);

        Content = _cardControl;
    }
}
