using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Dashboard;

public class AlwaysOnUSBControl : AbstractComboBoxFeatureCardControl<AlwaysOnUSBState>
{
    public AlwaysOnUSBControl()
    {
        Icon = SymbolRegular.UsbStick24;
        Title = Resource.AlwaysOnUSBControl_Title;
        Subtitle = Resource.AlwaysOnUSBControl_Message;
    }
}

