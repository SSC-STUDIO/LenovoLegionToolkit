using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;
using Button = UniversalDeviceToolkit.Avalonia.Controls.Button;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation;

public abstract class AbstractAutomationStepControl<T>(T automationStep) : AbstractAutomationStepControl(automationStep)
    where T : IAutomationStep
{
    protected new T AutomationStep => (T)base.AutomationStep;
}

public abstract class AbstractAutomationStepControl : UserControl
{
    protected IAutomationStep AutomationStep { get; }

    private readonly CardControl _cardControl = new()
    {
        Margin = new(0, 0, 0, 8),
    };

    private readonly CardHeaderControl _cardHeaderControl = new();

    private readonly StackPanel _stackPanel = new()
    {
        Orientation = Orientation.Horizontal,
    };

    private readonly Button _deleteButton = new()
    {
        Icon = new SymbolIcon { Symbol = SymbolRegular.Dismiss24 },
        MinWidth = 34,
        Height = 34,
        Margin = new(8, 0, 0, 0),
    };

    private SymbolRegular _icon = SymbolRegular.Empty;

    public SymbolRegular Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            _cardControl.Icon = new SymbolIcon { Symbol = value };
        }
    }

    public string Title
    {
        get => _cardHeaderControl.Title;
        set => _cardHeaderControl.Title = value;
    }

    public string Subtitle
    {
        get => _cardHeaderControl.Subtitle;
        set => _cardHeaderControl.Subtitle = value;
    }

    public VerticalAlignment TitleVerticalAlignment
    {
        get => _cardHeaderControl.TitleVerticalAlignment;
        set => _cardHeaderControl.TitleVerticalAlignment = value;
    }

    public VerticalAlignment SubtitleVerticalAlignment
    {
        get => _cardHeaderControl.SubtitleVerticalAlignment;
        set => _cardHeaderControl.SubtitleVerticalAlignment = value;
    }

    public event EventHandler? Changed;
    public event EventHandler? Delete;

    protected AbstractAutomationStepControl(IAutomationStep automationStep)
    {
        AutomationStep = automationStep;

        InitializeComponent();

        Loaded += RefreshingControl_Loaded;
    }

    private void InitializeComponent()
    {
        ToolTip.SetTip(_deleteButton, Resource.AbstractAutomationStepControl_Delete);
        _deleteButton.Click += (_, _) => Delete?.Invoke(this, EventArgs.Empty);

        var control = GetCustomControl();
        if (control is not null)
            _stackPanel.Children.Add(control);
        _stackPanel.Children.Add(_deleteButton);

        _cardHeaderControl.Accessory = _stackPanel;
        _cardControl.Header = _cardHeaderControl;

        Content = _cardControl;
    }

    private async void RefreshingControl_Loaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            await RefreshAsync();
            OnFinishedLoading();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(RefreshingControl_Loaded)}.", ex);
        }
    }

    public abstract IAutomationStep CreateAutomationStep();

    protected abstract Control? GetCustomControl();

    protected abstract void OnFinishedLoading();

    protected abstract Task RefreshAsync();

    protected void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
