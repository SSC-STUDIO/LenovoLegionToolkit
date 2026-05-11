using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.WPF.Resources;

namespace LenovoLegionToolkit.WPF.Controls.KeyboardBacklight.RGB;

public class RGBKeyboardBacklightSpeedCardControl : AbstractComboBoxRGBKeyboardCardControl<RGBKeyboardBacklightSpeed>
{
    public RGBKeyboardBacklightSpeedCardControl()
    {
        Icon = SymbolRegular.Keyboard24;
        Title = Resource.RGBKeyboardBacklightSpeedCardControl_Title;
    }
}
