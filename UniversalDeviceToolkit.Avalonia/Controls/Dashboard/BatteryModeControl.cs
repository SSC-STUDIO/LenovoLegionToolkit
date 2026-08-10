using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Resources;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard;

public class BatteryModeControl : AbstractComboBoxFeatureCardControl<BatteryState>
{
    public BatteryModeControl()
    {
        Icon = SymbolRegular.BatteryCharge24;
        Title = Resource.BatteryModeControl_Title;
        Subtitle = Resource.BatteryModeControl_Message;
    }
}
