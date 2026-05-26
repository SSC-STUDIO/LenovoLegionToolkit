using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation.Steps;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Automation.Steps;

public class BatteryNightChargeAutomationStepControl : AbstractComboBoxAutomationStepCardControl<BatteryNightChargeState>
{
    public BatteryNightChargeAutomationStepControl(IAutomationStep<BatteryNightChargeState> step) : base(step)
    {
        Icon = SymbolRegular.WeatherMoon24;
        Title = Resource.BatteryNightChargeAutomationStepControl_Title;
        Subtitle = Resource.BatteryNightChargeAutomationStepControl_Message;
    }
}

