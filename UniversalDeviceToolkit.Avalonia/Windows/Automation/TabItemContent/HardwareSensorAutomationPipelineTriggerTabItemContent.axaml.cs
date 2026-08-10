using System;
using UniversalDeviceToolkit.Lib.Automation.Pipeline.Triggers;
using UniversalDeviceToolkit.Avalonia.Extensions;

namespace UniversalDeviceToolkit.Avalonia.Windows.Automation.TabItemContent;

public partial class HardwareSensorAutomationPipelineTriggerTabItemContent : global::Avalonia.Controls.UserControl, IAutomationPipelineTriggerTabItemContent<HardwareSensorAutomationPipelineTrigger>
{
    private readonly HardwareSensorAutomationPipelineTrigger _trigger;

    public HardwareSensorAutomationPipelineTriggerTabItemContent(HardwareSensorAutomationPipelineTrigger trigger)
    {
        _trigger = trigger;
        InitializeComponent();
    }

    private void HardwareSensorAutomationPipelineTriggerTabItemContent_Initialized(object? sender, EventArgs e)
    {
        _metricComboBox.SetItems(Enum.GetValues<HardwareSensorMetric>(), _trigger.Metric, value => value.ToString());
        _comparisonComboBox.SetItems(Enum.GetValues<HardwareSensorComparison>(), _trigger.Comparison, value => value.ToString());
        _thresholdBox.Value = _trigger.Threshold;
        _durationSecondsBox.Value = _trigger.Duration.TotalSeconds;
        _cooldownSecondsBox.Value = _trigger.Cooldown.TotalSeconds;
    }

    public HardwareSensorAutomationPipelineTrigger GetTrigger()
    {
        var metric = _metricComboBox.TryGetSelectedItem(out HardwareSensorMetric selectedMetric)
            ? selectedMetric
            : _trigger.Metric;
        var comparison = _comparisonComboBox.TryGetSelectedItem(out HardwareSensorComparison selectedComparison)
            ? selectedComparison
            : _trigger.Comparison;
        var threshold = (float)(_thresholdBox.Value ?? _trigger.Threshold);
        var duration = TimeSpan.FromSeconds(_durationSecondsBox.Value ?? _trigger.Duration.TotalSeconds);
        var cooldown = TimeSpan.FromSeconds(_cooldownSecondsBox.Value ?? _trigger.Cooldown.TotalSeconds);
        return _trigger.DeepCopy(metric, comparison, threshold, duration, cooldown);
    }
}
