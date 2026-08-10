using System;
using System.Threading.Tasks;
using Avalonia.Automation;
using Avalonia.Controls;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation;

public abstract class AbstractComboBoxAutomationStepCardControl<T>(IAutomationStep<T> step)
    : AbstractAutomationStepControl<IAutomationStep<T>>(step) where T : struct
{
    private readonly ComboBox _comboBox = new()
    {
        MinWidth = 150,
        IsVisible = false,
        Margin = new(8, 0, 0, 0)
    };

    private T _state;

    protected override Control GetCustomControl()
    {
        _comboBox.SelectionChanged += ComboBox_SelectionChanged;

        return _comboBox;
    }

    private void ComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_comboBox.TryGetSelectedItem(out T selectedState) || _state.Equals(selectedState))
            return;

        _state = selectedState;

        RaiseChanged();
    }

    public override IAutomationStep CreateAutomationStep()
    {
        var obj = Activator.CreateInstance(AutomationStep.GetType(), _state);
        if (obj is not IAutomationStep<T> step)
            throw new InvalidOperationException(Resource.AutomationStep_CreationFailed);
        return step;
    }

    protected virtual string ComboBoxItemDisplayName(T value) => value switch
    {
        IDisplayName dn => dn.DisplayName,
        Enum e => e.GetDisplayName(),
        _ => value.ToString() ?? throw new InvalidOperationException(Resource.ComboBox_UnsupportedType)
    };

    protected override async Task RefreshAsync()
    {
        AutomationProperties.SetName(_comboBox, Title);

        var items = await AutomationStep.GetAllStatesAsync();
        var selectedItem = AutomationStep.State;

        _state = selectedItem;
        _comboBox.SetItems(items, selectedItem, ComboBoxItemDisplayName);
        _comboBox.IsEnabled = items.Length != 0;
    }

    protected override void OnFinishedLoading() => _comboBox.IsVisible = true;
}
