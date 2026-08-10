using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Resources;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard;

public class AlwaysOnUSBControl : AbstractComboBoxFeatureCardControl<AlwaysOnUSBState>
{
    public AlwaysOnUSBControl()
    {
        Icon = SymbolRegular.UsbStick24;
        Title = Resource.AlwaysOnUSBControl_Title;
        Subtitle = Resource.AlwaysOnUSBControl_Message;
    }
}
