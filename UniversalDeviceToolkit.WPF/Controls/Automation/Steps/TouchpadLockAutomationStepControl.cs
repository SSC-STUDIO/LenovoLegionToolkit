using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class TouchpadLockAutomationStepControl : AbstractComboBoxAutomationStepCardControl<TouchpadLockState>
{
    public TouchpadLockAutomationStepControl(IAutomationStep<TouchpadLockState> step) : base(step)
    {
        Icon = SymbolRegular.Tablet24;
        Title = Resource.TouchpadLockAutomationStepControl_Title;
        Subtitle = Resource.TouchpadLockAutomationStepControl_Message;
    }
}

