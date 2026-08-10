using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class GodModePresetAutomationStepControl : AbstractAutomationStepControl<GodModePresetAutomationStep>
{
    private readonly ComboBox _comboBox = new()
    {
        MinWidth = 150,
        IsVisible = false,
        Margin = new(8, 0, 0, 0)
    };

    public GodModePresetAutomationStepControl(GodModePresetAutomationStep step) : base(step)
    {
        Icon = SymbolRegular.Gauge24;
        Title = Resource.GodModePresetAutomationStepControl_Title;
        Subtitle = Resource.GodModePresetAutomationStepControl_Message;
    }

    public override IAutomationStep CreateAutomationStep()
    {
        if (_comboBox.TryGetSelectedItem(out KeyValuePair<Guid, GodModePreset> value))
            return new GodModePresetAutomationStep(value.Key);

        return new GodModePresetAutomationStep(AutomationStep.PresetId);
    }

    protected override Control GetCustomControl()
    {
        _comboBox.SelectionChanged += ComboBox_SelectionChanged;
        return _comboBox;
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => RaiseChanged();

    protected override async Task RefreshAsync()
    {
        var state = await AutomationStep.GetStateAsync();
        var presets = state.Presets
            .OrderBy(kv => kv.Value.Name)
            .ToArray();
        var selectedPreset = ResolveSelectedPreset(state, AutomationStep.PresetId);

        _comboBox.SetItems(presets, selectedPreset, kv => kv.Value.Name);
        _comboBox.IsEnabled = presets.Length != 0;
    }

    protected override void OnFinishedLoading() => _comboBox.IsVisible = true;

    internal static KeyValuePair<Guid, GodModePreset> ResolveSelectedPreset(GodModeState state, Guid requestedPresetId)
    {
        if (state.Presets.TryGetValue(requestedPresetId, out var requestedPreset))
            return new(requestedPresetId, requestedPreset);

        if (state.Presets.TryGetValue(state.ActivePresetId, out var activePreset))
            return new(state.ActivePresetId, activePreset);

        return state.Presets
            .OrderBy(kv => kv.Value.Name)
            .FirstOrDefault();
    }
}
