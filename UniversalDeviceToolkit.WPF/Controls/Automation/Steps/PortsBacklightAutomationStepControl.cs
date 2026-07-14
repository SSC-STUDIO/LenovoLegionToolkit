using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class PortsBacklightAutomationStepControl : AbstractComboBoxAutomationStepCardControl<PortsBacklightState>
{
    public PortsBacklightAutomationStepControl(IAutomationStep<PortsBacklightState> step) : base(step)
    {
        Icon = SymbolRegular.UsbPlug24;
        Title = Resource.PortsBacklightAutomationStepControl_Title;
        Subtitle = Resource.PortsBacklightAutomationStepControl_Message;
    }
}

