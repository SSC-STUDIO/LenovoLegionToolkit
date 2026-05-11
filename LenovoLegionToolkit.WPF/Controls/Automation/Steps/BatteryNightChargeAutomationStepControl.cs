using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Automation.Steps;
using LenovoLegionToolkit.WPF.Resources;

namespace LenovoLegionToolkit.WPF.Controls.Automation.Steps;

public class BatteryNightChargeAutomationStepControl : AbstractComboBoxAutomationStepCardControl<BatteryNightChargeState>
{
    public BatteryNightChargeAutomationStepControl(IAutomationStep<BatteryNightChargeState> step) : base(step)
    {
        Icon = SymbolRegular.WeatherMoon24;
        Title = Resource.BatteryNightChargeAutomationStepControl_Title;
        Subtitle = Resource.BatteryNightChargeAutomationStepControl_Message;
    }
}
