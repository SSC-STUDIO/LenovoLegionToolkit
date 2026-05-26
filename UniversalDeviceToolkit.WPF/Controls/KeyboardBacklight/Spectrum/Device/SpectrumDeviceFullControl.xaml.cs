using LenovoLegionToolkit.Lib;

namespace UniversalDeviceToolkit.WPF.Controls.KeyboardBacklight.Spectrum.Device
{
public partial class SpectrumDeviceFullControl
{
    public SpectrumDeviceFullControl()
    {
        InitializeComponent();
    }

    public void SetLayout(KeyboardLayout keyboardLayout)
    {
        _keyboard.SetLayout(keyboardLayout);
    }
}
}
