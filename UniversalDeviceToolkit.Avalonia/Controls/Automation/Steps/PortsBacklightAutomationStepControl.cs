using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class PortsBacklightAutomationStepControl : AbstractComboBoxAutomationStepCardControl<PortsBacklightState>
{
    public PortsBacklightAutomationStepControl(IAutomationStep<PortsBacklightState> step) : base(step)
    {
        Icon = SymbolRegular.UsbPlug24;
        Title = Resource.PortsBacklightAutomationStepControl_Title;
        Subtitle = Resource.PortsBacklightAutomationStepControl_Message;
    }
}
