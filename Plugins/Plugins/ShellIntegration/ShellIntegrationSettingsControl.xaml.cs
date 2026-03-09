using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace LenovoLegionToolkit.Plugins.ShellIntegration;

public partial class ShellIntegrationSettingsControl : UserControl
{
    private readonly ShellIntegrationPlugin _plugin;
    private bool _isHydrating;
    private ShellIntegrationProfile _profile = ShellIntegrationProfile.CreateDefault();

    public ShellIntegrationSettingsControl(ShellIntegrationPlugin plugin)
    {
        _plugin = plugin;
        TryInitializeComponent();
        Loaded += ShellIntegrationSettingsControl_Loaded;
    }

    private void TryInitializeComponent()
    {
        try
        {
            InitializeComponent();
        }
        catch
        {
            Content = new TextBlock
            {
                Margin = new Thickness(18),
                Text = ShellIntegrationText.FallbackLoadError,
                TextWrapping = TextWrapping.Wrap
            };
        }
    }

    private async void ShellIntegrationSettingsControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (Content is not Grid)
            return;

        _profile = _plugin.LoadProfile().Normalize();
        HydrateControlsFromProfile(_profile);
        UpdatePreview();
        await RefreshStatusAsync().ConfigureAwait(true);
    }

    private async Task RefreshStatusAsync(string? suffix = null)
    {
        if (_statusTextBlock is null || _toggleShellButton is null)
            return;

        var status = await _plugin.GetStatusAsync().ConfigureAwait(true);
        var detectedText = status.IsInstalled ? ShellIntegrationText.StatusDetected : ShellIntegrationText.StatusNotDetected;
        var registeredText = status.IsRegistered ? ShellIntegrationText.StatusRegistered : ShellIntegrationText.StatusUnregistered;

        _statusTextBlock.Text = $"{detectedText} {registeredText}";
        _statusDetailTextBlock.Text = string.IsNullOrWhiteSpace(suffix)
            ? ShellIntegrationText.StatusDetailDefault
            : suffix;
        _pathTextBlock.Text = $"{ShellIntegrationText.PathLabel}: {status.InstallPath ?? ShellIntegrationText.NotFound}";
        _managedPathTextBlock.Text = status.ManagedConfigDirectory is null
            ? ShellIntegrationText.ManagedConfigNotReady
            : $"{ShellIntegrationText.ManagedConfigLabel}: {status.ManagedConfigDirectory}";

        _statusBadgeTextBlock.Text = status.IsRegistered
            ? ShellIntegrationText.StatusEnabledBadge
            : status.IsInstalled
                ? ShellIntegrationText.StatusDisabledBadge
                : ShellIntegrationText.StatusMissingBadge;

        var badgeColor = status.IsRegistered ? "#1F43C66A" : status.IsInstalled ? "#1FCA8A04" : "#1FB42318";
        _statusBadgeBorder.Background = ToBrush(badgeColor, "#1F43C66A");

        _toggleShellButton.Content = status.IsRegistered ? ShellIntegrationText.DisableButton : ShellIntegrationText.EnableButton;
        _toggleShellButton.Background = ToBrush(status.IsRegistered ? "#B42318" : "#1D4ED8", "#1D4ED8");
        _toggleShellButton.BorderBrush = _toggleShellButton.Background;
        _toggleShellButton.Foreground = Brushes.White;
    }

    private async void ToggleShellButton_Click(object sender, RoutedEventArgs e)
    {
        var profile = ReadProfileFromControls();
        var currentStatus = await _plugin.GetStatusAsync().ConfigureAwait(true);
        profile.EnableShellIntegration = !currentStatus.IsRegistered;

        var result = await _plugin.SaveProfileAndApplyAsync(profile).ConfigureAwait(true);
        _profile = profile.Normalize();
        HydrateControlsFromProfile(_profile);
        UpdatePreview();
        await RefreshStatusAsync(result.Message).ConfigureAwait(true);
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        _applyButton.IsEnabled = false;
        try
        {
            var profile = ReadProfileFromControls();
            var result = await _plugin.SaveProfileAndApplyAsync(profile).ConfigureAwait(true);
            _profile = profile.Normalize();
            await RefreshStatusAsync(result.Message).ConfigureAwait(true);
        }
        finally
        {
            _applyButton.IsEnabled = true;
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshStatusAsync().ConfigureAwait(true);
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _profile = ShellIntegrationProfile.CreateDefault();
        HydrateControlsFromProfile(_profile);
        UpdatePreview();
    }

    private void PreviewInputsChanged(object sender, EventArgs e)
    {
        if (_isHydrating)
            return;

        UpdatePreview();
    }

    private void OpenStyleButton_Click(object sender, RoutedEventArgs e)
    {
        _plugin.OpenManagedConfigFolder();
        _statusDetailTextBlock.Text = ShellIntegrationText.StatusOpenedStyleSettings;
    }

    private void HydrateControlsFromProfile(ShellIntegrationProfile profile)
    {
        _isHydrating = true;
        try
        {
            SelectComboValue(_colorSchemeComboBox, profile.ColorScheme.ToString());
            SelectComboValue(_effectComboBox, profile.BackgroundEffect.ToString());
            _motionEffectsCheckBox.IsChecked = profile.EnableMotionEffects;
            _shadowCheckBox.IsChecked = profile.EnableShadow;
            _showDelaySlider.Value = profile.ShowDelay;
            _shadowOpacitySlider.Value = profile.ShadowOpacity;
            _accentColorTextBox.Text = profile.AccentColor;
            _backgroundColorTextBox.Text = profile.BackgroundColor;
            _hoverColorTextBox.Text = profile.HoverColor;
            _textColorTextBox.Text = profile.TextColor;
            _mutedColorTextBox.Text = profile.MutedTextColor;
            _tintColorTextBox.Text = profile.TintColor;
        }
        finally
        {
            _isHydrating = false;
        }
    }

    private ShellIntegrationProfile ReadProfileFromControls()
    {
        var profile = _profile.Normalize();
        profile.ColorScheme = ParseEnum(_colorSchemeComboBox, ShellColorScheme.Auto);
        profile.BackgroundEffect = ParseEnum(_effectComboBox, ShellVisualEffect.Acrylic);
        profile.EnableMotionEffects = _motionEffectsCheckBox.IsChecked == true;
        profile.EnableShadow = _shadowCheckBox.IsChecked == true;
        profile.ShowDelay = (int)_showDelaySlider.Value;
        profile.ShadowOpacity = (int)_shadowOpacitySlider.Value;
        profile.AccentColor = ShellIntegrationProfile.NormalizeHexColor(_accentColorTextBox.Text, profile.AccentColor);
        profile.BackgroundColor = ShellIntegrationProfile.NormalizeHexColor(_backgroundColorTextBox.Text, profile.BackgroundColor);
        profile.HoverColor = ShellIntegrationProfile.NormalizeHexColor(_hoverColorTextBox.Text, profile.HoverColor);
        profile.TextColor = ShellIntegrationProfile.NormalizeHexColor(_textColorTextBox.Text, profile.TextColor);
        profile.MutedTextColor = ShellIntegrationProfile.NormalizeHexColor(_mutedColorTextBox.Text, profile.MutedTextColor);
        profile.TintColor = ShellIntegrationProfile.NormalizeHexColor(_tintColorTextBox.Text, profile.TintColor);
        return profile.Normalize();
    }

    private void UpdatePreview()
    {
        var profile = ReadProfileFromControls();
        _showDelayValueTextBlock.Text = string.Format(ShellIntegrationText.ShowDelayValueFormat, profile.ShowDelay);
        _shadowValueTextBlock.Text = string.Format(ShellIntegrationText.ShadowStrengthValueFormat, profile.ShadowOpacity);

        var accentBrush = ToBrush(profile.AccentColor, "#4F7CFF");
        var backgroundBrush = ToBrush(profile.BackgroundColor, "#F7F8FC");
        var hoverBrush = ToBrush(profile.HoverColor, "#E8EEFF");
        var textBrush = ToBrush(profile.TextColor, "#111827");
        var mutedBrush = ToBrush(profile.MutedTextColor, "#667085");
        var selectedTextBrush = ToBrush(profile.SelectedTextColor, "#FFFFFF");

        _previewHostBorder.Background = backgroundBrush;
        _previewAccentBar.Background = accentBrush;
        _previewItemPrimary.Background = accentBrush;
        _previewItemSecondary.Background = hoverBrush;
        _previewItemTertiary.Background = backgroundBrush;

        _previewPrimaryText.Foreground = selectedTextBrush;
        _previewPrimaryHint.Foreground = selectedTextBrush;
        _previewSecondaryText.Foreground = textBrush;
        _previewSecondaryHint.Foreground = mutedBrush;
        _previewTertiaryText.Foreground = textBrush;
        _previewTertiaryHint.Foreground = mutedBrush;

        _previewHostBorder.Effect = profile.EnableShadow
            ? new DropShadowEffect
            {
                BlurRadius = 18,
                Direction = 270,
                ShadowDepth = 4,
                Opacity = Math.Clamp(profile.ShadowOpacity / 100.0, 0.0, 0.6),
                Color = ((SolidColorBrush)accentBrush).Color
            }
            : null;

        UpdatePreviewAnimation(profile.EnableMotionEffects);
    }

    private void UpdatePreviewAnimation(bool enabled)
    {
        _previewTranslateTransform.BeginAnimation(TranslateTransform.XProperty, null);
        _previewAccentBar.BeginAnimation(OpacityProperty, null);

        if (!enabled)
        {
            _previewTranslateTransform.X = 0;
            _previewAccentBar.Opacity = 1;
            return;
        }

        var movement = new DoubleAnimation
        {
            From = 0,
            To = 6,
            Duration = TimeSpan.FromSeconds(1.6),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        _previewTranslateTransform.BeginAnimation(TranslateTransform.XProperty, movement);

        var accentPulse = new DoubleAnimation
        {
            From = 0.72,
            To = 1.0,
            Duration = TimeSpan.FromSeconds(1.2),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        _previewAccentBar.BeginAnimation(OpacityProperty, accentPulse);
    }

    private static void SelectComboValue(ComboBox comboBox, string value)
    {
        foreach (var item in comboBox.Items)
        {
            if (item is ComboBoxItem comboBoxItem &&
                string.Equals(Convert.ToString(comboBoxItem.Tag), value, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = comboBoxItem;
                return;
            }
        }
    }

    private static TEnum ParseEnum<TEnum>(ComboBox comboBox, TEnum fallback) where TEnum : struct
    {
        if (comboBox.SelectedItem is ComboBoxItem item &&
            Enum.TryParse<TEnum>(Convert.ToString(item.Tag), true, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static Brush ToBrush(string color, string fallback)
    {
        var safeColor = ShellIntegrationProfile.NormalizeHexColor(color, fallback);
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(safeColor));
    }
}
