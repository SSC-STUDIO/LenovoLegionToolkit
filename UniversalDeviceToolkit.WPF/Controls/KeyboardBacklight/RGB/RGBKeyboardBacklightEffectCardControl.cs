using UniversalDeviceToolkit.WPF.Resources;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.KeyboardBacklight.RGB;

public class RGBKeyboardBacklightEffectCardControl : AbstractComboBoxRGBKeyboardCardControl<RGBKeyboardBacklightEffect>
{
    public RGBKeyboardBacklightEffectCardControl()
    {
        Icon = SymbolRegular.Keyboard24;
        Title = Resource.RGBKeyboardBacklightEffectCardControl_Title;
    }
}

