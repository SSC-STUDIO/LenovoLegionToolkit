using System;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Lib.Utils;
using Color = Avalonia.Media.Color;

namespace UniversalDeviceToolkit.Avalonia.Controls
{
public partial class ColorPickerControl : global::Avalonia.Controls.UserControl
{
    [GeneratedRegex("^#(?:[0-9A-F]{3}){2}$", RegexOptions.IgnoreCase, "en-DK")]
    private static partial Regex HexTextRegex();

    private bool CanHandleEvent => !_isEditing && _colorPicker is not null && _redNumberBox is not null && _greenNumberBox is not null && _blueNumberBox is not null && _hexTextBox is not null;

    private bool _isEditing;
    private bool _isPickerPointerDown;
    private readonly SolidColorBrush _buttonBrush = new(Colors.Aqua);
    private readonly DebounceDispatcher _debouncer = new();
    public Color SelectedColor
    {
        get => _colorPicker.Color;
        set => _colorPicker.Color = value;
    }

    /// <summary>Content rendered on the circular color button (for example, an eyedropper icon).</summary>
    public object? ButtonContent
    {
        get => _button.Content;
        set => _button.Content = value;
    }

    /// <summary>Tooltip shown for the color button.</summary>
    public object? ButtonToolTip
    {
        get => ToolTip.GetTip(_button);
        set => ToolTip.SetTip(_button, value);
    }

    /// <summary>Gets or sets the diameter of the circular color button.</summary>
    public double ButtonSize
    {
        get => _button.Width;
        set
        {
            _button.Width = value;
            _button.Height = value;
            _popup.HorizontalOffset = (value - _popup.Width) / 2;
        }
    }

    public Brush? ButtonBorderBrush
    {
        get => _button.BorderBrush as Brush;
        set => _button.BorderBrush = value;
    }

    public Thickness ButtonBorderThickness
    {
        get => _button.BorderThickness;
        set => _button.BorderThickness = value;
    }

    // AVALONIA: WPF Effect/DropShadowEffect have no Avalonia equivalent; kept as object
    // for source compatibility with callers (value is ignored).
    public object? ButtonEffect
    {
        get;
        set;
    }

    public event EventHandler? ColorChangedContinuous;
    public event EventHandler? ColorChangedDelayed;

    public ColorPickerControl()
    {
        InitializeComponent();

        _button.Background = _buttonBrush;
        SelectedColor = Colors.Aqua;
    }

    private void Button_Click(object? sender, RoutedEventArgs e)
    {
        _popup.IsOpen = true;
        e.Handled = true;
    }

    private void ColorPicker_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isPickerPointerDown = false;
        e.Handled = true;
        ColorChangedDelayed?.Invoke(this, EventArgs.Empty);
    }

    private void ColorPicker_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint((Visual)sender!).Properties;
        _isPickerPointerDown = properties.IsLeftButtonPressed || properties.IsRightButtonPressed;
        e.Handled = true;
    }

    private void ColorPicker_ColorChanged(object? sender, ColorChangedEventArgs e)
    {
        if (!CanHandleEvent)
            return;

        _isEditing = true;

        var color = _colorPicker.Color;

        _buttonBrush.Color = color;

        _redNumberBox.Text = color.R.ToString();
        _greenNumberBox.Text = color.G.ToString();
        _blueNumberBox.Text = color.B.ToString();

        _hexTextBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        ColorChangedContinuous?.Invoke(this, EventArgs.Empty);

        _isEditing = false;
    }


    private void NumberBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!CanHandleEvent)
            return;

        _debouncer.Debounce(300, () =>
        {
            _isEditing = true;

            var r = ToByte(_redNumberBox.Text);
            var g = ToByte(_greenNumberBox.Text);
            var b = ToByte(_blueNumberBox.Text);
            var color = Color.FromRgb(r, g, b);

            _buttonBrush.Color = color;

            _hexTextBox.Text = $"#{r:X2}{g:X2}{b:X2}";

            if (!_isPickerPointerDown)
            {
                _colorPicker.Color = color;

                ColorChangedDelayed?.Invoke(this, EventArgs.Empty);
            }

            _isEditing = false;
        });
    }

    private void HexTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!CanHandleEvent)
            return;

        if (!HexTextRegex().Match(_hexTextBox.Text ?? string.Empty).Success)
            return;

        _debouncer.Debounce(300, () =>
        {
            _isEditing = true;

            try
            {
                // AVALONIA: System.Drawing.ColorTranslator replaced with manual hex parsing
                // (the regex above already guarantees a valid 6-digit hex string).
                var hex = _hexTextBox.Text.TrimStart('#');
                var color = Color.FromRgb(
                    Convert.ToByte(hex.Substring(0, 2), 16),
                    Convert.ToByte(hex.Substring(2, 2), 16),
                    Convert.ToByte(hex.Substring(4, 2), 16));

                _buttonBrush.Color = color;

                _redNumberBox.Text = color.R.ToString();
                _greenNumberBox.Text = color.G.ToString();
                _blueNumberBox.Text = color.B.ToString();

                if (!_isPickerPointerDown)
                {
                    _colorPicker.Color = color;

                    ColorChangedDelayed?.Invoke(this, EventArgs.Empty);
                }
            }
            catch
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Failed to update color picker");
            }

            _isEditing = false;
        });
    }

    private void OK_Click(object? sender, RoutedEventArgs e) => _popup.IsOpen = false;

    private static byte ToByte(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return 0;

        if (!int.TryParse(s, out var userInput))
            return 0;

        return (byte)Math.Clamp(userInput, 0, 255);
    }
}
}
