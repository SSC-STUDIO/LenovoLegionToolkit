using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class OsdAutomationStepControl : AbstractComboBoxAutomationStepCardControl<OsdState>
{
    public OsdAutomationStepControl(IAutomationStep<OsdState> step) : base(step)
    {
        Icon = SymbolRegular.Window16;
        Title = Resource.OsdAutomationStepControl_Title;
        Subtitle = Resource.OsdAutomationStepControl_Message;
    }
}
