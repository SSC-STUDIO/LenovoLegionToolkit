using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Resources;

namespace UniversalDeviceToolkit.Avalonia.Controls.Dashboard;

public class InstantBootControl : AbstractComboBoxFeatureCardControl<InstantBootState>
{
    public InstantBootControl()
    {
        Icon = SymbolRegular.PlugDisconnected24;
        Title = Resource.InstantBootControl_Title;
        Subtitle = Resource.InstantBootControl_Message;
    }
}
