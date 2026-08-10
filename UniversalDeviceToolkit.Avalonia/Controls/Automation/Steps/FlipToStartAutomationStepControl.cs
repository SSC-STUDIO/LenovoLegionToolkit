using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class FlipToStartAutomationStepControl : AbstractComboBoxAutomationStepCardControl<FlipToStartState>
{
    public FlipToStartAutomationStepControl(IAutomationStep<FlipToStartState> step) : base(step)
    {
        Icon = SymbolRegular.Power24;
        Title = Resource.FlipToStartAutomationStepControl_Title;
        Subtitle = Resource.FlipToStartAutomationStepControl_Message;
    }
}
