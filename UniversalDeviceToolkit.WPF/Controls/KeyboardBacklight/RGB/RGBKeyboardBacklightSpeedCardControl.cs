using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.KeyboardBacklight.RGB;

public class RGBKeyboardBacklightSpeedCardControl : AbstractComboBoxRGBKeyboardCardControl<RGBKeyboardBacklightSpeed>
{
    public RGBKeyboardBacklightSpeedCardControl()
    {
        Icon = SymbolRegular.Keyboard24;
        Title = Resource.RGBKeyboardBacklightSpeedCardControl_Title;
    }
}

