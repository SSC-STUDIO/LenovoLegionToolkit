namespace UniversalDeviceToolkit.Avalonia.Controls.KeyboardBacklight.Spectrum.Device
{
public partial class SpectrumDeviceFullAlternativeControl : global::Avalonia.Controls.UserControl
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
