using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class OverDriveAutomationStepControl : AbstractComboBoxAutomationStepCardControl<OverDriveState>
{
    public OverDriveAutomationStepControl(IAutomationStep<OverDriveState> step) : base(step)
    {
        Icon = SymbolRegular.TopSpeed24;
        Title = Resource.OverDriveAutomationStepControl_Title;
        Subtitle = Resource.OverDriveAutomationStepControl_Message;
    }
}

