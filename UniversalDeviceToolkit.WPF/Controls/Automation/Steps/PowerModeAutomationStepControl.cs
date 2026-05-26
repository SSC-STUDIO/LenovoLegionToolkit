using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class PowerModeAutomationStepControl : AbstractComboBoxAutomationStepCardControl<PowerModeState>
{
    public PowerModeAutomationStepControl(IAutomationStep<PowerModeState> step) : base(step)
    {
        Icon = SymbolRegular.Gauge24;
        Title = Resource.PowerModeAutomationStepControl_Title;
        Subtitle = Resource.PowerModeAutomationStepControl_Message;
    }
}

