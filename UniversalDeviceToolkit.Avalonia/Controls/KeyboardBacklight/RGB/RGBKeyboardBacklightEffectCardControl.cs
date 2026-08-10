using UniversalDeviceToolkit.Avalonia.Resources;

namespace UniversalDeviceToolkit.Avalonia.Controls.KeyboardBacklight.RGB;

public class RGBKeyboardBacklightEffectCardControl : AbstractComboBoxRGBKeyboardCardControl<RGBKeyboardBacklightEffect>
{
    public RGBKeyboardBacklightEffectCardControl()
    {
        Icon = SymbolRegular.Keyboard24;
        Title = Resource.RGBKeyboardBacklightEffectCardControl_Title;
    }
}
