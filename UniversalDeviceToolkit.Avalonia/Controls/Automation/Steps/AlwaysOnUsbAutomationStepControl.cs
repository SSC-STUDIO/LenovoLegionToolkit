using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class AlwaysOnUsbAutomationStepControl : AbstractComboBoxAutomationStepCardControl<AlwaysOnUSBState>
{
    public AlwaysOnUsbAutomationStepControl(IAutomationStep<AlwaysOnUSBState> step) : base(step)
    {
        Icon = SymbolRegular.UsbStick24;
        Title = Resource.AlwaysOnUsbAutomationStepControl_Title;
        Subtitle = Resource.AlwaysOnUsbAutomationStepControl_Message;
    }
}
