using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Lib.Extensions;

namespace UniversalDeviceToolkit.Avalonia.Windows.KeyboardBacklight.Spectrum
{
public partial class SpectrumKeyboardBacklightEditEffectWindow : global::UniversalDeviceToolkit.Avalonia.Windows.BaseWindow
{
    private readonly ushort[] _keyCodes;
    private readonly ushort[] _allKeyboardKeyCodes;

    public event EventHandler<SpectrumKeyboardBacklightEffect>? Apply;

    public SpectrumKeyboardBacklightEditEffectWindow(ushort[] keyCodes, ushort[] allKeyboardKeyCodes)
    {
        _keyCodes = keyCodes;
        _allKeyboardKeyCodes = allKeyboardKeyCodes;

        InitializeComponent();

        _title.Text = Resource.SpectrumKeyboardBacklightEditEffectWindow_Title_Add;

        SetInitialValues();
        RefreshVisibility();
    }

    public SpectrumKeyboardBacklightEditEffectWindow(SpectrumKeyboardBacklightEffect effect, ushort[] keyCodes, ushort[] allKeyboardKeyCodes)
    {
        _keyCodes = effect.Type.IsAllLightsEffect() ? keyCodes : effect.Keys;
        _allKeyboardKeyCodes = allKeyboardKeyCodes;

        InitializeComponent();

        CanResize = false;
        CanMinimize = true;

        _title.Text = Resource.SpectrumKeyboardBacklightEditEffectWindow_Title_Add;

        _titleBar.CanMaximize = false;

        SetInitialValues();
        Update(effect);
        RefreshVisibility();
    }

    private void EffectsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshVisibility();

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        var effectType = SpectrumKeyboardBacklightEffectType.Always;
        var direction = SpectrumKeyboardBacklightDirection.None;
        var clockwiseDirection = SpectrumKeyboardBacklightClockwiseDirection.None;
        var speed = SpectrumKeyboardBacklightSpeed.None;
        var colors = Array.Empty<RGBColor>();

        if (_effectTypeCard.IsVisible &&
            _effectTypeComboBox.TryGetSelectedItem(out SpectrumKeyboardBacklightEffectType effectTypeTemp))
            effectType = effectTypeTemp;

        if (_directionCard.IsVisible &&
            _directionComboBox.TryGetSelectedItem(out SpectrumKeyboardBacklightDirection directionTemp))
            direction = directionTemp;

        if (_clockwiseDirectionCard.IsVisible &&
            _clockwiseDirectionComboBox.TryGetSelectedItem(out SpectrumKeyboardBacklightClockwiseDirection clockwiseDirectionTemp))
            clockwiseDirection = clockwiseDirectionTemp;

        if (_speedCard.IsVisible &&
            _speedComboBox.TryGetSelectedItem(out SpectrumKeyboardBacklightSpeed speedTemp))
            speed = speedTemp;

        if (_singleColor.IsVisible)
            colors = [_singleColorPicker.SelectedColor.ToRGBColor()];

        if (_multiColors.IsVisible)
            colors = _multiColorPicker.SelectedColors.Select(c => c.ToRGBColor()).ToArray();

        var keys = _keyCodes;

        if (effectType.IsAllLightsEffect())
            keys = [];
        if (effectType.IsWholeKeyboardEffect())
            keys = _allKeyboardKeyCodes;

        var effect = new SpectrumKeyboardBacklightEffect(effectType,
            speed,
            direction,
            clockwiseDirection,
            colors,
            keys);

        Apply?.Invoke(this, effect);
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private void SetInitialValues()
    {
        _effectTypeComboBox.SetItems(
            [
                SpectrumKeyboardBacklightEffectType.Always,
                SpectrumKeyboardBacklightEffectType.RainbowScrew,
                SpectrumKeyboardBacklightEffectType.RainbowWave,
                SpectrumKeyboardBacklightEffectType.ColorChange,
                SpectrumKeyboardBacklightEffectType.ColorWave,
                SpectrumKeyboardBacklightEffectType.ColorPulse,
                SpectrumKeyboardBacklightEffectType.Smooth,
                SpectrumKeyboardBacklightEffectType.Rain,
                SpectrumKeyboardBacklightEffectType.Ripple,
                SpectrumKeyboardBacklightEffectType.Type,
                SpectrumKeyboardBacklightEffectType.AudioBounce,
                SpectrumKeyboardBacklightEffectType.AudioRipple,
                SpectrumKeyboardBacklightEffectType.AuroraSync
            ],
            SpectrumKeyboardBacklightEffectType.Always,
            e => e.GetDisplayName());

        _directionComboBox.SetItems(
            [
                SpectrumKeyboardBacklightDirection.BottomToTop,
                SpectrumKeyboardBacklightDirection.TopToBottom,
                SpectrumKeyboardBacklightDirection.LeftToRight,
                SpectrumKeyboardBacklightDirection.RightToLeft
            ],
            SpectrumKeyboardBacklightDirection.BottomToTop,
            e => e.GetDisplayName());

        _clockwiseDirectionComboBox.SetItems(
            [
                SpectrumKeyboardBacklightClockwiseDirection.Clockwise,
                SpectrumKeyboardBacklightClockwiseDirection.CounterClockwise
            ],
            SpectrumKeyboardBacklightClockwiseDirection.Clockwise,
            e => e.GetDisplayName());

        _speedComboBox.SetItems(
            [
                SpectrumKeyboardBacklightSpeed.Speed1,
                SpectrumKeyboardBacklightSpeed.Speed2,
                SpectrumKeyboardBacklightSpeed.Speed3
            ],
            SpectrumKeyboardBacklightSpeed.Speed2,
            e => e.GetDisplayName());
    }

    private void Update(SpectrumKeyboardBacklightEffect effect)
    {
        if (_effectTypeComboBox.GetItems<SpectrumKeyboardBacklightEffectType>().Contains(effect.Type))
            _effectTypeComboBox.SelectItem(effect.Type);

        if (_directionComboBox.GetItems<SpectrumKeyboardBacklightDirection>().Contains(effect.Direction))
            _directionComboBox.SelectItem(effect.Direction);

        if (_clockwiseDirectionComboBox.GetItems<SpectrumKeyboardBacklightClockwiseDirection>()
            .Contains(effect.ClockwiseDirection))
            _clockwiseDirectionComboBox.SelectItem(effect.ClockwiseDirection);

        if (_speedComboBox.GetItems<SpectrumKeyboardBacklightSpeed>().Contains(effect.Speed))
            _speedComboBox.SelectItem(effect.Speed);

        var colors = effect.Colors.Select(c => Color.FromRgb(c.R, c.G, c.B)).ToArray();
        if (colors.Length != 0)
        {
            _singleColorPicker.SelectedColor = colors.First();
            _multiColorPicker.SelectedColors = colors;
        }
    }

    private void RefreshVisibility()
    {
        if (!_effectTypeComboBox.TryGetSelectedItem(out SpectrumKeyboardBacklightEffectType effect))
            return;

        _effectTypeCardHeader.Warning = effect.IsAllLightsEffect() || effect.IsWholeKeyboardEffect()
            ? Resource.SpectrumKeyboardBacklightEditEffectWindow_Effect_Warning
            : string.Empty;

        _directionCard.IsVisible = effect switch
        {
            SpectrumKeyboardBacklightEffectType.ColorWave => true,
            SpectrumKeyboardBacklightEffectType.RainbowWave => true,
            _ => false
        };

        _clockwiseDirectionCard.IsVisible = effect switch
        {
            SpectrumKeyboardBacklightEffectType.RainbowScrew => true,
            _ => false
        };

        _speedCard.IsVisible = effect switch
        {
            SpectrumKeyboardBacklightEffectType.ColorChange => true,
            SpectrumKeyboardBacklightEffectType.ColorPulse => true,
            SpectrumKeyboardBacklightEffectType.ColorWave => true,
            SpectrumKeyboardBacklightEffectType.Rain => true,
            SpectrumKeyboardBacklightEffectType.RainbowScrew => true,
            SpectrumKeyboardBacklightEffectType.RainbowWave => true,
            SpectrumKeyboardBacklightEffectType.Ripple => true,
            SpectrumKeyboardBacklightEffectType.Smooth => true,
            SpectrumKeyboardBacklightEffectType.Type => true,
            _ => false
        };

        _singleColor.IsVisible = effect switch
        {
            SpectrumKeyboardBacklightEffectType.Always => true,
            _ => false
        };

        _multiColors.IsVisible = effect switch
        {
            SpectrumKeyboardBacklightEffectType.ColorChange => true,
            SpectrumKeyboardBacklightEffectType.ColorPulse => true,
            SpectrumKeyboardBacklightEffectType.ColorWave => true,
            SpectrumKeyboardBacklightEffectType.Rain => true,
            SpectrumKeyboardBacklightEffectType.Ripple => true,
            SpectrumKeyboardBacklightEffectType.Smooth => true,
            SpectrumKeyboardBacklightEffectType.Type => true,
            _ => false
        };
    }
}
}
