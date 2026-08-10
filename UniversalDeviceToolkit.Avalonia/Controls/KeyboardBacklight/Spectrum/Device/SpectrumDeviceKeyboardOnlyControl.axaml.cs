namespace UniversalDeviceToolkit.Avalonia.Controls.KeyboardBacklight.Spectrum.Device
{
public partial class SpectrumDeviceKeyboardOnlyControl : global::Avalonia.Controls.UserControl
{
    public SpectrumDeviceKeyboardOnlyControl()
    {
        InitializeComponent();
    }

    public void SetLayout(KeyboardLayout keyboardLayout)
    {
        _keyboard.SetLayout(keyboardLayout);
    }
}
}
