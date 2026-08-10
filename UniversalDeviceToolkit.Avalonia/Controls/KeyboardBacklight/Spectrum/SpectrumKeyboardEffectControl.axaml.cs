using System;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Lib.Extensions;

namespace UniversalDeviceToolkit.Avalonia.Controls.KeyboardBacklight.Spectrum
{
public partial class SpectrumKeyboardEffectControl : global::Avalonia.Controls.UserControl
{
    public new SpectrumKeyboardBacklightEffect Effect { get; }

    public event EventHandler? Click;
    public event EventHandler? Edit;
    public event EventHandler? Delete;

    public SpectrumKeyboardEffectControl(SpectrumKeyboardBacklightEffect effect)
    {
        Effect = effect;

        InitializeComponent();

        _cardHeaderControl.Title = effect.Type.GetDisplayName();

        var subtitle = string.Empty;
        if (effect.Type.IsAllLightsEffect())
            subtitle += Resource.SpectrumKeyboardEffectControl_Description_AllZones;
        else
            subtitle += string.Format(Resource.SpectrumKeyboardEffectControl_Description_Zones, effect.Keys.Length);
        _cardHeaderControl.Subtitle = subtitle;
    }

    private void ButtonBase_OnClick(object? sender, RoutedEventArgs e)
    {
        Click?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void Edit_Click(object? sender, RoutedEventArgs e)
    {
        Edit?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        Delete?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }
}
}
