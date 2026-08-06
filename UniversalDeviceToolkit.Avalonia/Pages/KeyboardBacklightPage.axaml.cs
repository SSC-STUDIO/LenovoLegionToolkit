using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.Abstractions.Localization;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class KeyboardBacklightPage : UserControl
{
    private readonly IPlatformServices _platformServices;
    private readonly List<SpectrumEffectEditor> _spectrumEditors = [];
    private readonly List<RgbZoneEditor> _rgbZones = [];
    private KeyboardLightingState? _state;
    private bool _isRefreshing;
    private static readonly string[] RgbEffects = ["Static", "Breath", "Smooth", "WaveRTL", "WaveLTR"];
    private static readonly string[] RgbSpeeds = ["Slowest", "Slow", "Fast", "Fastest"];
    private static readonly string[] RgbBrightnessLevels = ["Low", "High"];
    private static readonly string[] SpectrumEffectTypes = ["Always", "RainbowScrew", "RainbowWave", "ColorChange", "ColorWave", "ColorPulse", "Smooth", "Rain", "Ripple", "Type", "AudioBounce", "AudioRipple", "AuroraSync"];
    private static readonly string[] SpectrumSpeeds = ["None", "Speed1", "Speed2", "Speed3"];
    private static readonly string[] SpectrumDirections = ["None", "BottomToTop", "TopToBottom", "LeftToRight", "RightToLeft"];
    private static readonly string[] SpectrumClockwiseDirections = ["None", "Clockwise", "CounterClockwise"];

    public KeyboardBacklightPage(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
        InitializeComponent();
        AutomationProperties.SetName(this, AvaloniaLocalization.GetString("KeyboardBacklightPage_Title", "Keyboard Backlight"));
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            _isRefreshing = true;
            ErrorMessage.IsVisible = false;
            SpectrumPanel.IsVisible = false;
            RgbPanel.IsVisible = false;

            var state = await _platformServices.GetKeyboardLightingStateAsync().ConfigureAwait(true);
            _state = state;
            if (state is null)
            {
                SetStatus(false,
                    AvaloniaLocalization.GetString("FeaturePage_Unsupported", "Unavailable on this device"),
                    AvaloniaLocalization.GetString("KeyboardBacklightPage_NoCompatibleKeyboardsFound", "No compatible keyboards were found."));
                return;
            }

            SetStatus(true,
                state.Mode,
                state.Mode.Equals("Spectrum", StringComparison.OrdinalIgnoreCase)
                    ? $"Profile {state.SelectedProfile} loaded with {state.SpectrumEffects.Count} effect(s)."
                    : $"{state.RgbPresets.Count} RGB preset(s) loaded from the keyboard controller.");

            if (state.Mode.Equals("Spectrum", StringComparison.OrdinalIgnoreCase))
                BuildSpectrum(state);
            else if (state.Mode.Equals("RGB", StringComparison.OrdinalIgnoreCase))
                BuildRgb(state);
        }
        catch (Exception ex)
        {
            SetStatus(false,
                AvaloniaLocalization.GetString("FeaturePage_LoadFailed", "Unable to load keyboard state"),
                ex.Message);
            ErrorMessage.Text = ex.Message;
            ErrorMessage.IsVisible = true;
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void SetStatus(bool available, string title, string message)
    {
        StatusTitle.Text = title;
        StatusMessage.Text = message;
        StatusCard.Background = GetResource<IBrush>(available ? "StatusSuccessBackgroundBrush" : "StatusInfoBackgroundBrush");
        StatusCard.BorderBrush = GetResource<IBrush>(available ? "StatusSuccessBrush" : "StatusInfoBrush");
    }

    private void BuildSpectrum(KeyboardLightingState state)
    {
        SpectrumPanel.IsVisible = true;
        SpectrumProfiles.Children.Clear();
        for (var profile = 1; profile <= 6; profile++)
        {
            var button = new Button
            {
                Content = $"Profile {profile}",
                Tag = profile,
                Margin = new Thickness(0, 0, 8, 8),
                MinWidth = 96,
            };
            button.Classes.Set("active", profile == state.SelectedProfile);
            AutomationProperties.SetName(button, $"Spectrum profile {profile}");
            button.Click += SpectrumProfile_Click;
            SpectrumProfiles.Children.Add(button);
        }

        SpectrumBrightness.Value = Math.Clamp(state.Brightness, 0, 9);
        SpectrumBrightnessValue.Text = state.Brightness.ToString(CultureInfo.InvariantCulture);
        SpectrumLogo.IsChecked = state.LogoEnabled;
        _spectrumEditors.Clear();
        SpectrumEffects.Items.Clear();
        foreach (var effect in state.SpectrumEffects)
        {
            var editor = new SpectrumEffectEditor(effect);
            _spectrumEditors.Add(editor);
            SpectrumEffects.Items.Add(CreateSpectrumEffectCard(editor));
        }
    }

    private Border CreateSpectrumEffectCard(SpectrumEffectEditor editor)
    {
        var details = new StackPanel { Spacing = 8 };
        details.Children.Add(new LocalizedTextBlock
        {
            Text = "Effect type",
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Ellipsis,
            MaxLines = 1,
        });
        details.Children.Add(editor.Type);
        details.Children.Add(new LocalizedTextBlock
        {
            Text = "Speed / direction",
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Ellipsis,
            MaxLines = 1,
        });
        var motion = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 8 };
        motion.Children.Add(editor.Speed);
        Grid.SetColumn(editor.Direction, 1);
        motion.Children.Add(editor.Direction);
        details.Children.Add(motion);
        details.Children.Add(editor.ClockwiseDirection);
        details.Children.Add(new LocalizedTextBlock
        {
            Text = $"{editor.Original.Keys.Count} key(s), colors as #RRGGBB values",
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        });
        details.Children.Add(editor.Colors);

        var removeButton = new Button
        {
            Content = AvaloniaLocalization.GetString("Delete", "Delete"),
            HorizontalAlignment = HorizontalAlignment.Right,
            Tag = editor,
        };
        AutomationProperties.SetName(removeButton, AvaloniaLocalization.GetString("Delete", "Delete"));
        ToolTip.SetTip(removeButton, AvaloniaLocalization.GetString("Delete", "Delete"));
        removeButton.Click += RemoveSpectrumEffect_Click;
        details.Children.Add(removeButton);

        return new Border
        {
            Background = GetResource<IBrush>("CardBackgroundBrush"),
            BorderBrush = GetResource<IBrush>("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = GetResource<CornerRadius>("CornerRadiusControl"),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 8),
            Child = details,
        };
    }

    private async void SpectrumProfile_Click(object? sender, RoutedEventArgs e)
    {
        if (_isRefreshing || sender is not Button { Tag: int profile })
            return;

        await ApplyAsync(new KeyboardLightingUpdate("Spectrum", SelectedProfile: profile));
    }

    private void AddSpectrumEffect_Click(object? sender, RoutedEventArgs e)
    {
        if (_isRefreshing || _state is null)
            return;

        var editor = new SpectrumEffectEditor(new KeyboardSpectrumEffectState(
            "Always",
            "None",
            "None",
            "None",
            [new KeyboardColorState(255, 255, 255)],
            []));
        _spectrumEditors.Add(editor);
        SpectrumEffects.Items.Add(CreateSpectrumEffectCard(editor));
    }

    private async void RemoveSpectrumEffect_Click(object? sender, RoutedEventArgs e)
    {
        if (_isRefreshing || _state is null || sender is not Button { Tag: SpectrumEffectEditor editor })
            return;

        _spectrumEditors.Remove(editor);
        await ApplyAsync(new KeyboardLightingUpdate(
            "Spectrum",
            SelectedProfile: _state.SelectedProfile,
            Brightness: (int)SpectrumBrightness.Value,
            LogoEnabled: SpectrumLogo.IsChecked == true,
            SpectrumEffects: _spectrumEditors.Select(item => item.ToState()).ToArray()));
    }

    private async void ResetSpectrum_Click(object? sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        try
        {
            if (!await _platformServices.ResetKeyboardSpectrumProfileAsync().ConfigureAwait(true))
            {
                ErrorMessage.Text = AvaloniaLocalization.GetString(
                    "KeyboardBacklightPage_SaveFailed",
                    "The keyboard controller rejected this change.");
                ErrorMessage.IsVisible = true;
                return;
            }

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage.Text = ex.Message;
            ErrorMessage.IsVisible = true;
        }
    }

    private async void ExportSpectrum_Click(object? sender, RoutedEventArgs e)
    {
        if (_isRefreshing || _state is null)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = $"spectrum-profile-{_state.SelectedProfile}.json",
            DefaultExtension = "json",
            FileTypeChoices = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }],
        });
        var path = file?.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!await _platformServices.ExportKeyboardSpectrumProfileAsync(path).ConfigureAwait(true))
        {
            ErrorMessage.Text = AvaloniaLocalization.GetString(
                "KeyboardBacklightPage_SaveFailed",
                "The keyboard controller rejected this change.");
            ErrorMessage.IsVisible = true;
        }
    }

    private async void ImportSpectrum_Click(object? sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }],
        });
        var path = files.FirstOrDefault()?.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            if (!await _platformServices.ImportKeyboardSpectrumProfileAsync(path).ConfigureAwait(true))
            {
                ErrorMessage.Text = AvaloniaLocalization.GetString(
                    "KeyboardBacklightPage_SaveFailed",
                    "The keyboard controller rejected this change.");
                ErrorMessage.IsVisible = true;
                return;
            }

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage.Text = ex.Message;
            ErrorMessage.IsVisible = true;
        }
    }

    private void SpectrumBrightness_ValueChanged(object? sender, global::Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        SpectrumBrightnessValue.Text = ((int)e.NewValue).ToString(CultureInfo.InvariantCulture);
    }

    private void SpectrumLogo_Click(object? sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        _ = ApplyAsync(new KeyboardLightingUpdate("Spectrum", LogoEnabled: SpectrumLogo.IsChecked == true));
    }

    private async void SaveSpectrum_Click(object? sender, RoutedEventArgs e)
    {
        if (_state is null)
            return;

        var effects = _spectrumEditors.Select(editor => editor.ToState()).ToArray();
        await ApplyAsync(new KeyboardLightingUpdate(
            "Spectrum",
            SelectedProfile: _state.SelectedProfile,
            Brightness: (int)SpectrumBrightness.Value,
            LogoEnabled: SpectrumLogo.IsChecked == true,
            SpectrumEffects: effects));
    }

    private void BuildRgb(KeyboardLightingState state)
    {
        RgbPanel.IsVisible = true;
        RgbPresets.Children.Clear();
        var selected = state.RgbPresets.FirstOrDefault(preset => preset.IsSelected);
        foreach (var preset in state.RgbPresets)
        {
            var button = new Button
            {
                Content = preset.DisplayName,
                Tag = preset.Key,
                Margin = new Thickness(0, 0, 8, 8),
                MinWidth = 82,
            };
            button.Classes.Set("active", preset.IsSelected);
            AutomationProperties.SetName(button, $"RGB preset {preset.DisplayName}");
            button.Click += RgbPreset_Click;
            RgbPresets.Children.Add(button);
        }

        selected ??= state.RgbPresets.FirstOrDefault();
        if (selected is null)
            return;

        SetComboItems(RgbEffect, RgbEffects, selected.Effect);
        SetComboItems(RgbSpeed, RgbSpeeds, selected.Speed);
        SetComboItems(RgbBrightness, RgbBrightnessLevels, selected.Brightness);
        var editable = !selected.Key.Equals("Off", StringComparison.OrdinalIgnoreCase);
        RgbEffect.IsEnabled = editable;
        RgbSpeed.IsEnabled = editable;
        RgbBrightness.IsEnabled = editable;

        _rgbZones.Clear();
        RgbZones.Children.Clear();
        foreach (var (color, index) in selected.Zones.Select((color, index) => (color, index)))
        {
            var editor = new RgbZoneEditor($"Zone {index + 1}", color);
            editor.SynchronizeRequested += RgbZoneEditor_SynchronizeRequested;
            _rgbZones.Add(editor);
            RgbZones.Children.Add(editor.Container);
        }
    }

    private async void RgbZoneEditor_SynchronizeRequested(object? sender, EventArgs e)
    {
        if (_isRefreshing || sender is not RgbZoneEditor source || _state is null)
            return;

        var color = source.ReadColor();
        foreach (var zone in _rgbZones)
            zone.SetColor(color);

        var selected = _state.RgbPresets.FirstOrDefault(preset => preset.IsSelected);
        if (selected is null)
            return;

        await ApplyAsync(new KeyboardLightingUpdate(
            "RGB",
            RgbPreset: selected.Key,
            RgbEffect: RgbEffect.SelectedItem?.ToString(),
            RgbSpeed: RgbSpeed.SelectedItem?.ToString(),
            RgbBrightness: RgbBrightness.SelectedItem?.ToString(),
            RgbZones: _rgbZones.Select(zone => zone.ReadColor()).ToArray()));
    }

    private async void RgbPreset_Click(object? sender, RoutedEventArgs e)
    {
        if (_isRefreshing || sender is not Button { Tag: string preset })
            return;

        await ApplyAsync(new KeyboardLightingUpdate("RGB", RgbPreset: preset));
    }

    private void RgbSetting_Changed(object? sender, SelectionChangedEventArgs e)
    {
        // The Save button performs one atomic controller update for all RGB fields.
    }

    private async void SaveRgb_Click(object? sender, RoutedEventArgs e)
    {
        if (_state is null)
            return;

        var selected = _state.RgbPresets.FirstOrDefault(preset => preset.IsSelected);
        if (selected is null)
            return;

        var zones = _rgbZones.Select(zone => zone.ReadColor()).ToArray();
        await ApplyAsync(new KeyboardLightingUpdate(
            "RGB",
            RgbPreset: selected.Key,
            RgbEffect: RgbEffect.SelectedItem?.ToString(),
            RgbSpeed: RgbSpeed.SelectedItem?.ToString(),
            RgbBrightness: RgbBrightness.SelectedItem?.ToString(),
            RgbZones: zones));
    }

    private async Task ApplyAsync(KeyboardLightingUpdate update)
    {
        try
        {
            _isRefreshing = true;
            ErrorMessage.IsVisible = false;
            if (!await _platformServices.SetKeyboardLightingAsync(update).ConfigureAwait(true))
            {
                ErrorMessage.Text = AvaloniaLocalization.GetString("KeyboardBacklightPage_SaveFailed", "The keyboard controller rejected this change.");
                ErrorMessage.IsVisible = true;
                return;
            }

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage.Text = ex.Message;
            ErrorMessage.IsVisible = true;
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private static void SetComboItems(ComboBox combo, IReadOnlyList<string> values, string selected)
    {
        combo.ItemsSource = values;
        combo.SelectedItem = values.Contains(selected, StringComparer.OrdinalIgnoreCase)
            ? values.First(value => value.Equals(selected, StringComparison.OrdinalIgnoreCase))
            : values.FirstOrDefault();
    }

    private T GetResource<T>(object key)
    {
        if (this.TryFindResource(key, out var value) && value is T typedValue)
            return typedValue;

        if (typeof(T) == typeof(IBrush))
            return (T)(object)new SolidColorBrush(Colors.Transparent);
        if (typeof(T) == typeof(CornerRadius))
            return (T)(object)new CornerRadius(8);
        throw new InvalidOperationException($"Missing Avalonia resource '{key}'.");
    }

    private sealed class SpectrumEffectEditor
    {
        public KeyboardSpectrumEffectState Original { get; }
        public ComboBox Type { get; }
        public ComboBox Speed { get; }
        public ComboBox Direction { get; }
        public ComboBox ClockwiseDirection { get; }
        public TextBox Colors { get; }

        public SpectrumEffectEditor(KeyboardSpectrumEffectState effect)
        {
            Original = effect;
            Type = CreateCombo(SpectrumEffectTypes, effect.Type);
            Speed = CreateCombo(SpectrumSpeeds, effect.Speed);
            Direction = CreateCombo(SpectrumDirections, effect.Direction);
            ClockwiseDirection = CreateCombo(SpectrumClockwiseDirections, effect.ClockwiseDirection);
            Colors = new TextBox
            {
                Text = string.Join(", ", effect.Colors.Select(color => color.Hex)),
                Watermark = "#RRGGBB, #RRGGBB",
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
        }

        public KeyboardSpectrumEffectState ToState() => new(
            Type.SelectedItem?.ToString() ?? Original.Type,
            Speed.SelectedItem?.ToString() ?? Original.Speed,
            Direction.SelectedItem?.ToString() ?? Original.Direction,
            ClockwiseDirection.SelectedItem?.ToString() ?? Original.ClockwiseDirection,
            ParseColors(Colors.Text, Original.Colors),
            Original.Keys);

        private static ComboBox CreateCombo(IReadOnlyList<string> values, string selected) => new()
        {
            ItemsSource = values,
            SelectedItem = values.FirstOrDefault(value => value.Equals(selected, StringComparison.OrdinalIgnoreCase)) ?? values.FirstOrDefault(),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
    }

    private sealed class RgbZoneEditor
    {
        private readonly TextBox _input;
        private readonly Border _swatch;

        public event EventHandler? SynchronizeRequested;

        public Border Container { get; }

        public RgbZoneEditor(string title, KeyboardColorState color)
        {
            _input = new TextBox { Text = color.Hex, Width = 100, Watermark = "#RRGGBB" };
            _swatch = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(14),
                Background = ToBrush(color),
                Margin = new Thickness(0, 0, 8, 0),
            };
            _input.TextChanged += Input_TextChanged;
            var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            content.Children.Add(_swatch);
            content.Children.Add(_input);
            var synchronizeButton = new Button
            {
                Width = 32,
                Height = 32,
                Padding = new Thickness(4),
                Content = new NavigationIcon { IconIdentifier = "ArrowSync24" },
            };
            var synchronizeText = AvaloniaLocalization.GetString(
                "RGBKeyboardBacklightControl_SynchroniseZones",
                "Synchronize zones");
            AutomationProperties.SetName(synchronizeButton, synchronizeText);
            ToolTip.SetTip(synchronizeButton, synchronizeText);
            synchronizeButton.Click += (_, _) => SynchronizeRequested?.Invoke(this, EventArgs.Empty);
            content.Children.Add(synchronizeButton);
            Container = new Border
            {
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 8, 8),
                Background = new SolidColorBrush(Color.FromArgb(20, 128, 128, 128)),
                CornerRadius = new CornerRadius(6),
                Child = new StackPanel
                {
                    Spacing = 4,
                    Children = { new TextBlock { Text = title }, content },
                },
            };
        }

        public KeyboardColorState ReadColor() => ParseColor(_input.Text, new KeyboardColorState(255, 255, 255));

        public void SetColor(KeyboardColorState color) => _input.Text = color.Hex;

        private void Input_TextChanged(object? sender, TextChangedEventArgs e)
        {
            _swatch.Background = ToBrush(ParseColor(_input.Text, new KeyboardColorState(255, 255, 255)));
        }
    }

    private static IReadOnlyList<KeyboardColorState> ParseColors(string? value, IReadOnlyList<KeyboardColorState> fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var colors = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(text => ParseColor(text, null))
            .Where(color => color is not null)
            .Cast<KeyboardColorState>()
            .ToArray();
        return colors.Length == 0 ? fallback : colors;
    }

    private static KeyboardColorState ParseColor(string? value, KeyboardColorState? fallback)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var text = value.Trim().TrimStart('#');
            if (text.Length == 6 && uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
                return new KeyboardColorState((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        }

        return fallback ?? new KeyboardColorState(255, 255, 255);
    }

    private static IBrush ToBrush(KeyboardColorState color) =>
        new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
}
