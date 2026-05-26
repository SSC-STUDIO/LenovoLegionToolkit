using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard;

public class BatteryModeControl : AbstractComboBoxFeatureCardControl<BatteryState>
{
    public BatteryModeControl()
    {
        Icon = SymbolRegular.BatteryCharge24;
        Title = Resource.BatteryModeControl_Title;
        Subtitle = Resource.BatteryModeControl_Message;
    }
}

