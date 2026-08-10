using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Automation.Steps;

public class BatteryNightChargeAutomationStepControl : AbstractComboBoxAutomationStepCardControl<BatteryNightChargeState>
{
    public BatteryNightChargeAutomationStepControl(IAutomationStep<BatteryNightChargeState> step) : base(step)
    {
        Icon = SymbolRegular.WeatherMoon24;
        Title = Resource.BatteryNightChargeAutomationStepControl_Title;
        Subtitle = Resource.BatteryNightChargeAutomationStepControl_Message;
    }
}
