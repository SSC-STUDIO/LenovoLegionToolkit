using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class AlwaysOnUsbAutomationStepControl : AbstractComboBoxAutomationStepCardControl<AlwaysOnUSBState>
{
    public AlwaysOnUsbAutomationStepControl(IAutomationStep<AlwaysOnUSBState> step) : base(step)
    {
        Icon = SymbolRegular.UsbStick24;
        Title = Resource.AlwaysOnUsbAutomationStepControl_Title;
        Subtitle = Resource.AlwaysOnUsbAutomationStepControl_Message;
    }
}

