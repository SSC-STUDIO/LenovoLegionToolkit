using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.KeyboardBacklight.RGB;

public class RGBKeyboardBacklightBrightnessCardControl : AbstractComboBoxRGBKeyboardCardControl<RGBKeyboardBacklightBrightness>
{
    public RGBKeyboardBacklightBrightnessCardControl()
    {
        Icon = SymbolRegular.Keyboard24;
        Title = Resource.RGBKeyboardBacklightBrightnessCardControl_Brightness;
    }
}

