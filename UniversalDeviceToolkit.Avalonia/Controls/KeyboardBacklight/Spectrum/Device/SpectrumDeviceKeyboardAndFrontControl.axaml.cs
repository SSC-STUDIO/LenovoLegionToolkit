namespace UniversalDeviceToolkit.Avalonia.Controls.KeyboardBacklight.Spectrum.Device
{
public partial class SpectrumDeviceKeyboardAndFrontControl : global::Avalonia.Controls.UserControl
{
    public SpectrumDeviceKeyboardAndFrontControl()
    {
        InitializeComponent();
    }

    public void SetLayout(KeyboardLayout keyboardLayout)
    {
        _keyboard.SetLayout(keyboardLayout);
    }
}
}
