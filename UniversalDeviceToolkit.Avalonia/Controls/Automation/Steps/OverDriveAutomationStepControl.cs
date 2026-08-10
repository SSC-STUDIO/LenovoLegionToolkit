using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class OverDriveAutomationStepControl : AbstractComboBoxAutomationStepCardControl<OverDriveState>
{
    public OverDriveAutomationStepControl(IAutomationStep<OverDriveState> step) : base(step)
    {
        Icon = SymbolRegular.TopSpeed24;
        Title = Resource.OverDriveAutomationStepControl_Title;
        Subtitle = Resource.OverDriveAutomationStepControl_Message;
    }
}
