using LenovoLegionToolkit.Lib;

namespace UniversalDeviceToolkit.WPF.Controls.KeyboardBacklight.Spectrum.Device
{
public partial class SpectrumDeviceFullAlternativeControl
{
    public SpectrumDeviceFullAlternativeControl()
    {
        InitializeComponent();
    }

    public void SetLayout(KeyboardLayout keyboardLayout)
    {
        _keyboard.SetLayout(keyboardLayout);
    }
}
}
