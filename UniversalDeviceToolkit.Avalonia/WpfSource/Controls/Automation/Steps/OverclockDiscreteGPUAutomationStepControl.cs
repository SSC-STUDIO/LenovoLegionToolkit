using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class OverclockDiscreteGPUAutomationStepControl : AbstractComboBoxAutomationStepCardControl<OverclockDiscreteGPUAutomationStepState>
{
    public OverclockDiscreteGPUAutomationStepControl(IAutomationStep<OverclockDiscreteGPUAutomationStepState> step) : base(step)
    {
        Icon = SymbolRegular.DeveloperBoardLightning20;
        Title = Resource.OverclockDiscreteGPUAutomationStepControl_Title;
        Subtitle = Resource.OverclockDiscreteGPUAutomationStepControl_Message;
    }
}

