using UniversalDeviceToolkit.Lib.Automation.Pipeline.Triggers;

namespace UniversalDeviceToolkit.Avalonia.Windows.Automation.TabItemContent;

public interface IAutomationPipelineTriggerTabItemContent<out T> where T : IAutomationPipelineTrigger
{
    T GetTrigger();
}
