using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers.GodMode;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.Lib.Automation.Pipeline.Triggers;

namespace UniversalDeviceToolkit.WPF.Windows.Automation.TabItemContent
{
public partial class GodModePresetPipelineTriggerTabItemContent : IAutomationPipelineTriggerTabItemContent<IGodModePresetChangedAutomationPipelineTrigger>
{
    private readonly GodModeController _godModeController = IoCContainer.Resolve<GodModeController>();

    private readonly IGodModePresetChangedAutomationPipelineTrigger _trigger;

    public GodModePresetPipelineTriggerTabItemContent(IGodModePresetChangedAutomationPipelineTrigger trigger)
    {
        _trigger = trigger;

        InitializeComponent();
    }

    public IGodModePresetChangedAutomationPipelineTrigger GetTrigger()
    {
        var state = _content.Children
            .OfType<RadioButton>()
            .Where(r => r.IsChecked ?? false)
            .Select(r => (Guid)r.Tag)
            .DefaultIfEmpty(Guid.Empty)
            .FirstOrDefault();
        return _trigger.DeepCopy(state);
    }

    private async void GodModePresetPipelineTriggerTabItemContent_Initialized(object? sender, EventArgs e)
    {
        IReadOnlyDictionary<Guid, GodModePreset> presets;
        try
        {
            presets = (await _godModeController.GetStateAsync().ConfigureAwait(false)).Presets;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to load God Mode presets for automation trigger configuration.", ex);
            presets = new Dictionary<Guid, GodModePreset>();
        }

        _content.Children.Clear();
        foreach (var (guid, preset) in presets.OrderBy(kv => kv.Value.Name))
        {
            var radio = new RadioButton
            {
                Content = preset.Name,
                Tag = guid,
                IsChecked = guid == _trigger.PresetId,
                Margin = new(0, 0, 0, 8)
            };
            _content.Children.Add(radio);
        }
    }
}
}
