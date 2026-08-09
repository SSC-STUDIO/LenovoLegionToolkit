using System;
using Avalonia.Input;
using Avalonia.Media;

namespace UniversalDeviceToolkit.Avalonia.Controls
{
public partial class MultiColorPickerItemControl : global::Avalonia.Controls.UserControl
{
    public Color SelectedColor
    {
        get => _picker.SelectedColor;
        set => _picker.SelectedColor = value;
    }

    public event EventHandler? ColorChangedContinuous
    {
        add => _picker.ColorChangedContinuous += value;
        remove => _picker.ColorChangedContinuous -= value;
    }

    public event EventHandler? ColorChangedDelayed
    {
        add => _picker.ColorChangedDelayed += value;
        remove => _picker.ColorChangedDelayed -= value;
    }

    public event EventHandler<PointerPressedEventArgs>? Delete;

    public MultiColorPickerItemControl() => InitializeComponent();

    private void Delete_Click(object? sender, PointerPressedEventArgs e)
    {
        // AVALONIA: WPF MouseLeftButtonDown/MouseRightButtonDown collapsed into a single
        // PointerPressed handler; check the button state here.
        var properties = e.GetCurrentPoint((global::Avalonia.Visual)sender!).Properties;
        if (!properties.IsLeftButtonPressed && !properties.IsRightButtonPressed)
            return;

        Delete?.Invoke(this, e);
    }
}
}
