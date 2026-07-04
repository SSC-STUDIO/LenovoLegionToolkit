using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Listeners;
using LenovoLegionToolkit.Lib.Messaging;
using LenovoLegionToolkit.Lib.Messaging.Messages;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.SoftwareDisabler;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Windows.KeyboardBacklight.Spectrum;
using Microsoft.Win32;
using NeoSmart.AsyncLock;

namespace UniversalDeviceToolkit.WPF.Controls.KeyboardBacklight.Spectrum
{
public partial class SpectrumKeyboardBacklightControl : AbstractRefreshingControl, IDisposable
{
    private static string T(string key, string fallback) => LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    private readonly ThrottleLastDispatcher _changeBrightnessDispatcher = new(TimeSpan.FromMilliseconds(250), "ChangeBrightnessDispatcher");
    private readonly TimeSpan _refreshStateInterval = TimeSpan.FromMilliseconds(50);
    private readonly AsyncLock _startStopAnimationLock = new();

    private readonly SpectrumKeyboardBacklightController _controller = IoCContainer.Resolve<SpectrumKeyboardBacklightController>();
    private readonly SpecialKeyListener _listener = IoCContainer.Resolve<SpecialKeyListener>();
    private readonly VantageDisabler _vantageDisabler = IoCContainer.Resolve<VantageDisabler>();
    private readonly SpectrumKeyboardSettings _settings = IoCContainer.Resolve<SpectrumKeyboardSettings>();
    private bool _subscribedToMessagingCenter;

    private CancellationTokenSource? _refreshStateCancellationTokenSource;
    private Task? _refreshStateTask;

    private RadioButton[] ProfileButtons =>
    [
        _profileButton1,
        _profileButton2,
        _profileButton3,
        _profileButton4,
        _profileButton5,
        _profileButton6
    ];

    protected override bool DisablesWhileRefreshing => false;

    public SpectrumKeyboardBacklightControl()
    {
        InitializeComponent();

        IsVisibleChanged += SpectrumKeyboardBacklightControl_IsVisibleChanged;
        SizeChanged += SpectrumKeyboardBacklightControl_SizeChanged;

        _listener.Changed += Listener_Changed;

        MessagingCenter.Subscribe<SpectrumBacklightChangedMessage>(this, OnSpectrumBacklightChanged);

        Unloaded += SpectrumKeyboardBacklightControl_Unloaded;
    }

    private void OnSpectrumBacklightChanged()
    {
        Dispatcher.InvokeTask(async () =>
        {
            if (!IsVisible)
                return;

            await RefreshBrightnessAsync().ConfigureAwait(false);
            await RefreshProfileAsync().ConfigureAwait(false);
            await RefreshProfileDescriptionAsync().ConfigureAwait(false);
        }, "spectrum backlight changed");
    }

    private async void SpectrumKeyboardBacklightControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
            return;

        await StopAnimationAsync().ConfigureAwait(false);
        _effects.Children.Clear();
    }

    private void SpectrumKeyboardBacklightControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_device.LayoutTransform is not ScaleTransform scaleTransform)
            return;

        var target = 0.75 * ActualWidth / _device.ActualWidth;
        var scale = Math.Clamp(target, 0.5, 1.5);

        scaleTransform.ScaleX = scale;
        scaleTransform.ScaleY = scale;
    }

    private void SpectrumKeyboardBacklightControl_Unloaded(object sender, RoutedEventArgs e)
    {
        MessagingCenter.Unsubscribe<SpectrumBacklightChangedMessage>(this);
        _listener.Changed -= Listener_Changed;
        IsVisibleChanged -= SpectrumKeyboardBacklightControl_IsVisibleChanged;
        SizeChanged -= SpectrumKeyboardBacklightControl_SizeChanged;

        Dispose();
    }

    public void Dispose()
    {
        _changeBrightnessDispatcher.Dispose();
        _refreshStateCancellationTokenSource?.Dispose();
        _refreshStateCancellationTokenSource = null;
    }

    private void Listener_Changed(object? sender, SpecialKeyListener.ChangedEventArgs e) => Dispatcher.InvokeTask(async () =>
    {
        if (!IsLoaded || !IsVisible)
            return;

        if (!await _controller.IsSupportedAsync().ConfigureAwait(false))
            return;

        if (await _vantageDisabler.GetStatusAsync().ConfigureAwait(false) == SoftwareStatus.Enabled)
            return;

        switch (e.SpecialKey)
        {
            case SpecialKey.SpectrumBacklightOff
                or SpecialKey.SpectrumBacklight1
                or SpecialKey.SpectrumBacklight2
                or SpecialKey.SpectrumBacklight3:
                await RefreshBrightnessAsync().ConfigureAwait(false);
                break;
            case SpecialKey.SpectrumPreset1
                or SpecialKey.SpectrumPreset2
                or SpecialKey.SpectrumPreset3
                or SpecialKey.SpectrumPreset4
                or SpecialKey.SpectrumPreset5
                or SpecialKey.SpectrumPreset6:
                await RefreshProfileAsync().ConfigureAwait(false);
                break;
        }
    }, "refresh spectrum keyboard backlight control");

    private async void BrightnessSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => await _changeBrightnessDispatcher.DispatchAsync(async () =>
    {
        var value = await Dispatcher.InvokeAsync(() => (int)_brightnessSlider.Value);
        if (await _controller.GetBrightnessAsync().ConfigureAwait(false) != value)
            await _controller.SetBrightnessAsync(value).ConfigureAwait(false);
    });

    private async void ProfileButton_OnClick(object sender, RoutedEventArgs e)
    {
        await StopAnimationAsync().ConfigureAwait(false);

        if ((sender as RadioButton)?.Tag is not int profile)
            return;

        _brightnessSlider.IsEnabled = false;
        foreach (var profileButton in ProfileButtons)
            profileButton.IsEnabled = false;

        if (await _controller.GetProfileAsync().ConfigureAwait(false) != profile)
        {
            await _controller.SetProfileAsync(profile).ConfigureAwait(false);
            await RefreshProfileDescriptionAsync().ConfigureAwait(false);
        }

        foreach (var profileButton in ProfileButtons)
            profileButton.IsEnabled = true;
        _brightnessSlider.IsEnabled = true;

        if (IsVisible)
            await StartAnimationAsync().ConfigureAwait(false);
    }

    private void SelectableControl_Selected(object? sender, SelectableControl.SelectedEventArgs e)
    {
        foreach (var button in _device.GetVisibleButtons().Where(b => !(b.IsChecked ?? false)))
            button.IsChecked = e.ContainsCenter(button);
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e) => SelectAllButtons();

    private void DeselectAll_Click(object sender, RoutedEventArgs e) => DeselectAllButtons();

    private async void SwitchKeyboardLayout_Click(object sender, RoutedEventArgs e)
    {
        await StopAnimationAsync().ConfigureAwait(false);

        var buttons = _device.GetVisibleButtons();
        foreach (var button in buttons)
            button.IsChecked = false;

        var currentKeyboardLayout = _settings.Store.KeyboardLayout;
        var keyboardLayout = currentKeyboardLayout switch
        {
            KeyboardLayout.Ansi => KeyboardLayout.Iso,
            KeyboardLayout.Iso => KeyboardLayout.Jis,
            KeyboardLayout.Jis => KeyboardLayout.Ansi,
            _ => throw new ArgumentException(nameof(currentKeyboardLayout))
        };

        _settings.Store.KeyboardLayout = keyboardLayout;
        _settings.SynchronizeStore();

        var (spectrumLayout, _, keys) = await _controller.GetKeyboardLayoutAsync().ConfigureAwait(false);

        _device.SetLayout(spectrumLayout, keyboardLayout, keys);

        if (IsVisible)
            await StartAnimationAsync().ConfigureAwait(false);
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var sfd = new SaveFileDialog
            {
                Title = Resource.Export,
                InitialDirectory = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}",
                Filter = T("Common_JsonFileDialogFilter", "Json Files (.json)|*.json"),
            };

            var result = sfd.ShowDialog();

            if (!result.HasValue || !result.Value)
                return;

            var profile = await _controller.GetProfileAsync().ConfigureAwait(false);
            await _controller.ExportProfileDescriptionAsync(profile, sfd.FileName).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Couldn't export profile.", ex);

            await SnackbarHelper.ShowAsync(Resource.SpectrumKeyboardBacklightControl_ExportProfileError_Title, Resource.SpectrumKeyboardBacklightControl_ExportProfileError_Message, SnackbarType.Error).ConfigureAwait(false);
        }
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var ofd = new OpenFileDialog
            {
                Title = Resource.Import,
                InitialDirectory = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}",
                Filter = T("Common_JsonFileDialogFilter", "Json Files (.json)|*.json"),
                CheckFileExists = true,
            };

            var result = ofd.ShowDialog() ?? false;
            if (!result)
                return;

            var profile = await _controller.GetProfileAsync().ConfigureAwait(false);
            await _controller.ImportProfileDescription(profile, ofd.FileName).ConfigureAwait(false);

            await RefreshProfileDescriptionAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Couldn't import profile.", ex);

            await SnackbarHelper.ShowAsync(Resource.SpectrumKeyboardBacklightControl_ImportProfileError_Title, Resource.SpectrumKeyboardBacklightControl_ImportProfileError_Message, SnackbarType.Error).ConfigureAwait(false);
        }
    }

    private void AddEffectButton_Click(object sender, RoutedEventArgs e)
    {
        var buttons = _device.GetVisibleButtons().ToArray();
        var checkedButtons = buttons.Where(b => b.IsChecked ?? false).ToArray();

        if (checkedButtons.IsEmpty())
        {
            SelectAllButtons();
            checkedButtons = buttons;
        }

        var keyCodes = checkedButtons.Select(b => b.KeyCode).ToArray();

        var allKeyboardKeyCodes = _device.GetVisibleKeyboardButtons()
            .Select(b => b.KeyCode)
            .ToArray();

        CreateEffect(keyCodes, allKeyboardKeyCodes);
    }

    private async void ResetToDefaultButton_Click(object sender, RoutedEventArgs e) => await ResetToDefaultAsync().ConfigureAwait(false);

    protected override async Task OnRefreshAsync()
    {
        if (!await _controller.IsSupportedAsync().ConfigureAwait(false))
            throw new InvalidOperationException("Spectrum Keyboard does not seem to be supported");

        var vantageStatus = await _vantageDisabler.GetStatusAsync().ConfigureAwait(false);
        if (vantageStatus == SoftwareStatus.Enabled)
        {
            _vantageWarningInfoBar.IsOpen = true;

            _device.SetLayout(SpectrumLayout.Full, KeyboardLayout.Ansi, []);
            _content.IsEnabled = false;

            _noEffectsText.Visibility = Visibility.Collapsed;
            return;
        }

        _vantageWarningInfoBar.IsOpen = false;

        var (spectrumLayout, keyboardLayout, keys) = await _controller.GetKeyboardLayoutAsync().ConfigureAwait(false);

        if (!_settings.Store.KeyboardLayout.HasValue)
        {
            _settings.Store.KeyboardLayout = keyboardLayout;
            _settings.SynchronizeStore();
        }
        else
        {
            keyboardLayout = _settings.Store.KeyboardLayout.Value;
        }

        _device.SetLayout(spectrumLayout, keyboardLayout, keys);

        _content.IsEnabled = true;

        await RefreshBrightnessAsync().ConfigureAwait(false);
        await RefreshProfileAsync().ConfigureAwait(false);

        if (IsVisible)
            await StartAnimationAsync().ConfigureAwait(false);
    }

    protected override void OnFinishedLoading() { }

    private void SelectButtons(SpectrumKeyboardBacklightEffect effect)
    {
        if (effect.Type.IsAllLightsEffect())
        {
            SelectAllButtons();
            return;
        }

        DeselectAllButtons();

        foreach (var button in _device.GetVisibleButtons())
        {
            if (!effect.Keys.Contains(button.KeyCode))
                continue;

            button.IsChecked = true;
        }
    }

    private void SelectAllButtons()
    {
        foreach (var button in _device.GetVisibleButtons())
            button.IsChecked = true;
    }

    private void DeselectAllButtons()
    {
        foreach (var button in _device.GetVisibleButtons())
            button.IsChecked = false;
    }

    private async Task StartAnimationAsync()
    {
        using (await _startStopAnimationLock.LockAsync())
        {
            await StopAnimationAsync().ConfigureAwait(false);

            if (_refreshStateCancellationTokenSource is not null)
                await _refreshStateCancellationTokenSource.CancelAsync().ConfigureAwait(false);

            _refreshStateCancellationTokenSource = new();

            _refreshStateTask = RefreshStateAsync(_refreshStateCancellationTokenSource.Token);
        }
    }

    private async Task StopAnimationAsync()
    {
        using (await _startStopAnimationLock.LockAsync())
        {
            if (_refreshStateCancellationTokenSource is not null)
                await _refreshStateCancellationTokenSource.CancelAsync().ConfigureAwait(false);

            _refreshStateCancellationTokenSource = new();

            if (_refreshStateTask is not null)
                await _refreshStateTask.ConfigureAwait(false);

            _refreshStateTask = null;
        }
    }

    private async Task RefreshStateAsync(CancellationToken token)
    {
        var buttons = _device.GetVisibleButtons().ToArray();

        if (buttons.Length < 1)
            return;

        var firstCheck = true;

        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();

                if (!IsVisible)
                    break;

                var delay = Task.Delay(_refreshStateInterval, token);
                var state = await Task.Run(() => _controller.GetStateAsync(!firstCheck), token).ConfigureAwait(false);

                foreach (var button in buttons)
                {
                    if (!state.TryGetValue(button.KeyCode, out var rgb))
                    {
                        button.Color = null;
                        continue;
                    }

                    if (rgb is { R: < 1, G: < 1, B: < 1 })
                    {
                        button.Color = null;
                        continue;
                    }

                    button.Color = Color.FromRgb(rgb.R, rgb.G, rgb.B);
                }

                await delay.ConfigureAwait(false);

                firstCheck = false;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when state refresh is cancelled, no action needed
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to refresh state.", ex);
        }
        finally
        {
            foreach (var button in buttons)
                button._background.Background = null;
        }
    }

    private async Task RefreshBrightnessAsync()
    {
        _brightnessSlider.Value = await _controller.GetBrightnessAsync().ConfigureAwait(false);
    }

    private async Task RefreshProfileAsync()
    {
        var profile = await _controller.GetProfileAsync().ConfigureAwait(false);
        var profileButton = ProfileButtons.FirstOrDefault(pb => pb.Tag.Equals(profile));
        if (profileButton is null)
            return;

        profileButton.IsChecked = true;

        await RefreshProfileDescriptionAsync().ConfigureAwait(false);
    }

    private async Task RefreshProfileDescriptionAsync()
    {
        var profile = await _controller.GetProfileAsync().ConfigureAwait(false);
        var (_, effects) = await _controller.GetProfileDescriptionAsync(profile).ConfigureAwait(false);

        DeleteAllEffects();

        foreach (var effect in effects)
        {
            var control = CreateEffectControl(effect);
            _effects.Children.Add(control);
        }

        _noEffectsText.Visibility = effects.IsEmpty() ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task ApplyProfileAsync()
    {
        var profile = await _controller.GetProfileAsync().ConfigureAwait(false);
        var effects = _effects.Children.OfType<SpectrumKeyboardEffectControl>().Select(c => c.Effect).ToArray();

        try
        {
            await _controller.SetProfileDescriptionAsync(profile, effects).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Couldn't apply profile.", ex);

            await SnackbarHelper.ShowAsync(Resource.SpectrumKeyboardBacklightControl_ApplyProfileError_Title, Resource.SpectrumKeyboardBacklightControl_ApplyProfileError_Message, SnackbarType.Error).ConfigureAwait(false);
        }

        await RefreshProfileDescriptionAsync().ConfigureAwait(false);
    }

    private async Task ResetToDefaultAsync()
    {
        DeselectAllButtons();

        var profile = await _controller.GetProfileAsync().ConfigureAwait(false);
        await _controller.SetProfileDefaultAsync(profile).ConfigureAwait(false);

        await RefreshProfileDescriptionAsync().ConfigureAwait(false);
    }

    private void CreateEffect(ushort[] keyCodes, ushort[] allKeyboardKeyCodes)
    {
        var window = new SpectrumKeyboardBacklightEditEffectWindow(keyCodes, allKeyboardKeyCodes) { Owner = Window.GetWindow(this) };
        window.Apply += async (_, e) => await AddEffect(e).ConfigureAwait(false);
        window.ShowDialog();
    }

    private SpectrumKeyboardEffectControl CreateEffectControl(SpectrumKeyboardBacklightEffect effect)
    {
        var control = new SpectrumKeyboardEffectControl(effect);
        control.Click += (_, _) => SelectButtons(effect);
        control.Edit += (_, _) => EditEffect(control);
        control.Delete += async (_, _) => await DeleteEffectAsync(control).ConfigureAwait(false);
        return control;
    }

    private async Task AddEffect(SpectrumKeyboardBacklightEffect effect)
    {
        DeselectAllButtons();

        var control = CreateEffectControl(effect);
        _effects.Children.Add(control);

        await ApplyProfileAsync().ConfigureAwait(false);
    }

    private void EditEffect(SpectrumKeyboardEffectControl effectControl)
    {
        var keyCodes = _device.GetVisibleButtons()
            .Select(b => b.KeyCode)
            .ToArray();
        var allKeyboardKeyCodes = _device.GetVisibleKeyboardButtons()
            .Select(b => b.KeyCode)
            .ToArray();

        var window = new SpectrumKeyboardBacklightEditEffectWindow(effectControl.Effect, keyCodes, allKeyboardKeyCodes) { Owner = Window.GetWindow(this) };
        window.Apply += async (_, e) => await ReplaceEffectAsync(effectControl, e).ConfigureAwait(false);
        window.ShowDialog();
    }

    private async Task ReplaceEffectAsync(UIElement effectControl, SpectrumKeyboardBacklightEffect effect)
    {
        DeselectAllButtons();

        var control = new SpectrumKeyboardEffectControl(effect);
        control.Click += (_, _) => SelectButtons(effect);
        control.Edit += (_, _) => EditEffect(control);
        control.Delete += async (_, _) => await DeleteEffectAsync(control).ConfigureAwait(false);

        var index = _effects.Children.IndexOf(effectControl);
        if (index < 0)
        {
            _effects.Children.Add(control);
        }
        else
        {
            _effects.Children.RemoveAt(index);
            _effects.Children.Insert(index, control);
        }

        await ApplyProfileAsync().ConfigureAwait(false);
    }

    private async Task DeleteEffectAsync(UIElement effectControl)
    {
        _effects.Children.Remove(effectControl);

        await ApplyProfileAsync().ConfigureAwait(false);
    }

    private void DeleteAllEffects() => _effects.Children.Clear();
}
}
