using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class DeactivateGPUAutomationStepControl : AbstractComboBoxAutomationStepCardControl<DeactivateGPUAutomationStepState>
{
    public DeactivateGPUAutomationStepControl(DeactivateGPUAutomationStep step) : base(step)
    {
        Icon = SymbolRegular.DeveloperBoard24;
        Title = Resource.DeactivateGPUAutomationStepControl_Title;
        Subtitle = Resource.DeactivateGPUAutomationStepControl_Message;
    }
}

