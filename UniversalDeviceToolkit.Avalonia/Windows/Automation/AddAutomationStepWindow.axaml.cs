using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Controls.Automation;

namespace UniversalDeviceToolkit.Avalonia.Windows.Automation
{
public partial class AddAutomationStepWindow : global::UniversalDeviceToolkit.Avalonia.Windows.BaseWindow
{
    private readonly List<AbstractAutomationStepControl> _controls;
    private readonly Action<AbstractAutomationStepControl> _addStepControl;

    public AddAutomationStepWindow(List<AbstractAutomationStepControl> controls, Action<AbstractAutomationStepControl> addStepControl)
    {
        _controls = controls;
        _addStepControl = addStepControl;

        InitializeComponent();

        PropertyChanged += AddAutomationStepWindow_PropertyChanged;
    }

    private async void AddAutomationStepWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
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
                Log.Instance.Trace($"Exception in {nameof(AddAutomationStepWindow_PropertyChanged)}.", ex);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

    private Task RefreshAsync()
    {
        _content.Children.Clear();

        foreach (var control in _controls)
            _content.Children.Add(CreateCardControl(control));

        return Task.CompletedTask;
    }

    private CardControl CreateCardControl(AbstractAutomationStepControl stepControl)
    {
        var control = new CardControl
        {
            Icon = new SymbolIcon { Symbol = stepControl.Icon },
            Header = new CardHeaderControl
            {
                Title = stepControl.Title,
                Accessory = new SymbolIcon { Symbol = SymbolRegular.ChevronRight24 }
            },
            Margin = new(0, 8, 0, 0),
        };

        control.Click += (_, _) =>
        {
            _addStepControl(stepControl);
            Close();
        };

        return control;
    }
}
}
