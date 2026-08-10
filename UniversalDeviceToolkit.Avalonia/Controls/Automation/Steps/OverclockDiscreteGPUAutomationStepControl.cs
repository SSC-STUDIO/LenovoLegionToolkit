using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class OverclockDiscreteGPUAutomationStepControl : AbstractComboBoxAutomationStepCardControl<OverclockDiscreteGPUAutomationStepState>
{
    public OverclockDiscreteGPUAutomationStepControl(IAutomationStep<OverclockDiscreteGPUAutomationStepState> step) : base(step)
    {
        Icon = SymbolRegular.DeveloperBoardLightning20;
        Title = Resource.OverclockDiscreteGPUAutomationStepControl_Title;
        Subtitle = Resource.OverclockDiscreteGPUAutomationStepControl_Message;
    }
}
