using System;
using UniversalDeviceToolkit.Lib.Automation.Pipeline.Triggers;
using UniversalDeviceToolkit.Avalonia.Extensions;

namespace UniversalDeviceToolkit.Avalonia.Windows.Automation.TabItemContent;

public partial class BatteryPercentageAutomationPipelineTriggerTabItemContent : global::Avalonia.Controls.UserControl, IAutomationPipelineTriggerTabItemContent<BatteryPercentageAutomationPipelineTrigger>
{
    private readonly BatteryPercentageAutomationPipelineTrigger _trigger;

    public BatteryPercentageAutomationPipelineTriggerTabItemContent(BatteryPercentageAutomationPipelineTrigger trigger)
    {
        _trigger = trigger;
        InitializeComponent();
    }

    private void BatteryPercentageAutomationPipelineTriggerTabItemContent_Initialized(object? sender, EventArgs e)
    {
        _comparisonComboBox.SetItems(Enum.GetValues<BatteryPercentageComparison>(), _trigger.Comparison, value => value.ToString());
        _chargeFilterComboBox.SetItems(Enum.GetValues<BatteryChargeFilter>(), _trigger.ChargeFilter, value => value.ToString());
        _thresholdBox.Value = _trigger.Threshold;
        _durationSecondsBox.Value = _trigger.Duration.TotalSeconds;
        _cooldownSecondsBox.Value = _trigger.Cooldown.TotalSeconds;
    }

    public BatteryPercentageAutomationPipelineTrigger GetTrigger()
    {
        var comparison = _comparisonComboBox.TryGetSelectedItem(out BatteryPercentageComparison selectedComparison)
            ? selectedComparison
            : _trigger.Comparison;
        var chargeFilter = _chargeFilterComboBox.TryGetSelectedItem(out BatteryChargeFilter selectedFilter)
            ? selectedFilter
            : _trigger.ChargeFilter;
        var threshold = (int)(_thresholdBox.Value ?? _trigger.Threshold);
        var duration = TimeSpan.FromSeconds(_durationSecondsBox.Value ?? _trigger.Duration.TotalSeconds);
        var cooldown = TimeSpan.FromSeconds(_cooldownSecondsBox.Value ?? _trigger.Cooldown.TotalSeconds);
        return _trigger.DeepCopy(comparison, threshold, duration, cooldown, chargeFilter);
    }
}
