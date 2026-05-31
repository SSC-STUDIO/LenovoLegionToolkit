using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class OsdAutomationStepControl : AbstractComboBoxAutomationStepCardControl<OsdState>
{
    public OsdAutomationStepControl(IAutomationStep<OsdState> step) : base(step)
    {
        Icon = SymbolRegular.Window16;
        Title = Resource.OsdAutomationStepControl_Title;
        Subtitle = Resource.OsdAutomationStepControl_Message;
    }
}
