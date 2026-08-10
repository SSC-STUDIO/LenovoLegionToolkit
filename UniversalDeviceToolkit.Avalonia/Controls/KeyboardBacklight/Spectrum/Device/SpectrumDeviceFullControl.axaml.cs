namespace UniversalDeviceToolkit.Avalonia.Controls.KeyboardBacklight.Spectrum.Device
{
public partial class SpectrumDeviceFullControl : global::Avalonia.Controls.UserControl
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
