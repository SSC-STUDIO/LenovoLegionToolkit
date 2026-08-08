using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class DelayAutomationStepControl : AbstractComboBoxAutomationStepCardControl<Delay>
{
    public DelayAutomationStepControl(IAutomationStep<Delay> step) : base(step)
    {
        Icon = SymbolRegular.Clock24;
        Title = Resource.DelayAutomationStepControl_Title;
        Subtitle = Resource.DelayAutomationStepControl_Message;
    }
}

