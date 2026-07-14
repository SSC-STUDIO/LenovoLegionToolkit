using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard;

public class BatteryNightChargeModeControl : AbstractToggleFeatureCardControl<BatteryNightChargeState>
{
    protected override BatteryNightChargeState OnState => BatteryNightChargeState.On;
    protected override BatteryNightChargeState OffState => BatteryNightChargeState.Off;

    public BatteryNightChargeModeControl()
    {
        Icon = SymbolRegular.WeatherMoon24;
        Title = Resource.BatteryNightChargeModeControl_Title;
        Subtitle = Resource.BatteryNightChargeModeControl_Message;
    }
}

