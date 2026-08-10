using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class PowerModeAutomationStepControl : AbstractComboBoxAutomationStepCardControl<PowerModeState>
{
    public PowerModeAutomationStepControl(IAutomationStep<PowerModeState> step) : base(step)
    {
        Icon = SymbolRegular.Gauge24;
        Title = Resource.PowerModeAutomationStepControl_Title;
        Subtitle = Resource.PowerModeAutomationStepControl_Message;
    }
}
