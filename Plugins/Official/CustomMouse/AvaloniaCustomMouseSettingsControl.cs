using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using System.Threading;

namespace UniversalDeviceToolkit.Plugins.CustomMouse;

/// <summary>
/// Native Avalonia settings surface for the Custom Mouse plugin.  It mirrors
/// the WPF page's persisted settings and actions while keeping the legacy WPF
/// control available to the WPF host.
/// </summary>
public sealed class AvaloniaCustomMouseSettingsControl : UserControl
{
    private readonly CustomMousePlugin _plugin;
    private readonly Slider _pointerSpeed;
    private readonly CheckBox _swapButtons;
    private readonly ComboBox _cursorTheme;
    private readonly TextBlock _speedValue;
    private readonly TextBlock _previewValue;
    private readonly TextBlock _buttonValue;
    private readonly TextBlock _themeValue;
    private readonly TextBlock _status;
    private readonly TextBlock _statusIcon;
    private readonly ProgressBar _applyProgress;
    private bool _hydrating;
    private int _cursorThemeSelectionVersion;

    public AvaloniaCustomMouseSettingsControl(CustomMousePlugin plugin)
    {
        _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        _pointerSpeed = new Slider { Minimum = 1, Maximum = 20, TickFrequency = 1, IsSnapToTickEnabled = true };
        _swapButtons = new CheckBox { Content = CustomMouseText.SwapButtonsLabel };
        _cursorTheme = new ComboBox
        {
            ItemsSource = new[]
            {
                CustomMouseText.CursorThemeModeAuto,
                CustomMouseText.CursorThemeModeLight,
                CustomMouseText.CursorThemeModeDark,
                CustomMouseText.CursorThemeModeWindowsDefault,
            },
            MinWidth = 220,
        };
        _speedValue = ValueText();
        _previewValue = ValueText();
        _buttonValue = ValueText();
        _themeValue = ValueText();
        _status = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Gray };
        _statusIcon = new TextBlock
        {
            Text = "OK",
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.Gray,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _applyProgress = new ProgressBar
        {
            IsIndeterminate = true,
            IsVisible = false,
            Height = 4,
            Margin = new Thickness(0, 2, 0, 0),
        };

        AutomationProperties.SetAutomationId(this, "AvaloniaCustomMouseSettingsRoot");
        Content = BuildContent();
        WireEvents();
        Loaded += (_, _) => Hydrate();
    }

    private Control BuildContent()
    {
        var root = new StackPanel { Spacing = 14, Margin = new Thickness(20, 16, 20, 20) };
        root.Children.Add(Card(
            CustomMouseText.SettingsOverviewTitle,
            CustomMouseText.WindowsSettingsDescription,
            new StackPanel
            {
                Children =
                {
                    Metric(CustomMouseText.PointerPreviewLabel, _previewValue),
                    Metric(CustomMouseText.ButtonLayoutLabel, _buttonValue),
                    Metric(CustomMouseText.CursorThemeStatusLabel, _themeValue),
                },
            }));

        var pointer = new StackPanel { Spacing = 10 };
        pointer.Children.Add(Heading(CustomMouseText.WindowsSettingsTitle));
        pointer.Children.Add(Labelled(CustomMouseText.PointerSpeedLabel, _pointerSpeed, _speedValue));
        pointer.Children.Add(_swapButtons);

        var theme = new StackPanel { Spacing = 10 };
        theme.Children.Add(Heading(CustomMouseText.CursorThemeModeLabel));
        theme.Children.Add(new TextBlock
        {
            Text = CustomMouseText.CursorHint,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Gray,
        });
        theme.Children.Add(_cursorTheme);
        theme.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                ActionButton(CustomMouseText.ApplyCursorThemeNowButton, () => ApplyCursorThemeAsync(), "AvaloniaCustomMouseApplyCursorTheme"),
                ActionButton(CustomMouseText.RestoreWindowsDefaultButton, RestoreWindowsDefaultAsync, "AvaloniaCustomMouseRestoreWindowsDefault"),
            },
        });

        var settingsGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,14,*"),
        };
        var pointerCard = Card(CustomMouseText.WindowsSettingsTitle, CustomMouseText.WindowsSettingsDescription, pointer);
        Grid.SetColumn(pointerCard, 0);
        settingsGrid.Children.Add(pointerCard);
        var themeCard = Card(CustomMouseText.CursorThemeModeLabel, CustomMouseText.CursorHint, theme);
        Grid.SetColumn(themeCard, 2);
        settingsGrid.Children.Add(themeCard);
        root.Children.Add(settingsGrid);

        var actions = new StackPanel { Spacing = 8 };
        actions.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children = { _statusIcon, _status },
        });
        actions.Children.Add(_applyProgress);
        actions.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                ActionButton(CustomMouseText.SyncFromWindowsButton, (Action)SyncFromWindows, "AvaloniaCustomMouseSyncFromWindows"),
                ActionButton(CustomMouseText.ReloadButton, (Action)ReloadSettings, "AvaloniaCustomMouseReload"),
                ActionButton(CustomMouseText.ApplyToWindowsButton, ApplyToWindows, "AvaloniaCustomMouseApply"),
            },
        });
        root.Children.Add(Card(CustomMouseText.ApplyToWindowsButton, CustomMouseText.SettingsSubtitle, actions));
        return new ScrollViewer { Content = root, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    }

    private void WireEvents()
    {
        AutomationProperties.SetAutomationId(_pointerSpeed, "AvaloniaCustomMousePointerSpeed");
        AutomationProperties.SetAutomationId(_swapButtons, "AvaloniaCustomMouseSwapButtons");
        AutomationProperties.SetAutomationId(_cursorTheme, "AvaloniaCustomMouseCursorTheme");
        AutomationProperties.SetAutomationId(_status, "AvaloniaCustomMouseStatus");
        AutomationProperties.SetAutomationId(_applyProgress, "AvaloniaCustomMouseApplyProgress");
        _pointerSpeed.ValueChanged += (_, _) =>
        {
            if (!_hydrating)
                RefreshSummary();
        };
        _swapButtons.IsCheckedChanged += (_, _) =>
        {
            if (!_hydrating)
                RefreshSummary();
        };
        _cursorTheme.SelectionChanged += async (_, _) =>
        {
            if (!_hydrating && _cursorTheme.SelectedIndex >= 0)
            {
                var selectionVersion = Interlocked.Increment(ref _cursorThemeSelectionVersion);
                await ApplyCursorThemeAsync(selectionVersion);
            }
        };
    }

    private void Hydrate(bool setReadyStatus = true)
    {
        _hydrating = true;
        _pointerSpeed.Value = _plugin.Settings.WindowsPointerSpeed;
        _swapButtons.IsChecked = _plugin.Settings.SwapButtons;
        _cursorTheme.SelectedIndex = (int)_plugin.Settings.CursorThemeMode;
        _hydrating = false;
        RefreshSummary();
        if (setReadyStatus)
            SetStatus(CustomMouseText.ProfileReady, false);
    }

    private async Task ApplyToWindows()
    {
        _applyProgress.IsVisible = true;
        SetStatus(CustomMouseText.StatusApplying, false);
        try
        {
            var speed = (int)Math.Round(_pointerSpeed.Value);
            var swapButtons = _swapButtons.IsChecked == true;
            var originalSpeed = _plugin.Settings.WindowsPointerSpeed;

            if (!_plugin.SetWindowsPointerSpeed(speed))
            {
                SetStatus(CustomMouseText.StatusApplyPointerFail, true);
                Hydrate(setReadyStatus: false);
                return;
            }

            if (!_plugin.SetSwapButtons(swapButtons))
            {
                if (_plugin.Settings.WindowsPointerSpeed != originalSpeed)
                    _plugin.SetWindowsPointerSpeed(originalSpeed);

                SetStatus(CustomMouseText.StatusApplySwapFail, true);
                Hydrate(setReadyStatus: false);
                return;
            }

            await _plugin.SaveSettingsAsync().ConfigureAwait(true);
            SetStatus(CustomMouseText.StatusWindowsApplied, false);
            RefreshSummary();
        }
        catch (Exception ex)
        {
            SetStatus($"{CustomMouseText.ApplyFailedPrefix} {ex.Message}", true);
        }
        finally
        {
            _applyProgress.IsVisible = false;
        }
    }

    private void SyncFromWindows()
    {
        _plugin.ReloadSettingsFromSystem();
        Hydrate();
        SetStatus(CustomMouseText.StatusSyncedFromWindows, false);
    }

    private void ReloadSettings()
    {
        Hydrate();
        SetStatus(CustomMouseText.StatusReloaded, false);
    }

    private async Task ApplyCursorThemeAsync(int? selectionVersion = null)
    {
        if (_cursorTheme.SelectedIndex < 0)
            return;

        var mode = (CursorThemeMode)_cursorTheme.SelectedIndex;
        try
        {
            var applied = await _plugin.SetCursorThemeModeAsync(mode).ConfigureAwait(true);
            if (selectionVersion.HasValue
                && selectionVersion.Value != Volatile.Read(ref _cursorThemeSelectionVersion))
            {
                return;
            }

            if (!applied)
            {
                _hydrating = true;
                _cursorTheme.SelectedIndex = (int)_plugin.Settings.CursorThemeMode;
                _hydrating = false;
            }

            SetStatus(
                applied ? CustomMouseText.StatusWindowsApplied : CustomMouseText.StatusCursorApplyFailed,
                !applied);
            RefreshSummary();
        }
        catch (Exception ex)
        {
            _hydrating = true;
            _cursorTheme.SelectedIndex = (int)_plugin.Settings.CursorThemeMode;
            _hydrating = false;
            SetStatus($"{CustomMouseText.StatusCursorApplyFailed}: {ex.Message}", true);
            RefreshSummary();
        }
    }

    private async Task RestoreWindowsDefaultAsync()
    {
        try
        {
            var restored = await _plugin.RestoreWindowsDefaultCursorThemeAsync().ConfigureAwait(true);
            Hydrate(setReadyStatus: false);
            SetStatus(
                restored
                    ? CustomMouseText.StatusWindowsDefaultRestored
                    : CustomMouseText.StatusRestoreWindowsDefaultFailed,
                !restored);
        }
        catch (Exception ex)
        {
            SetStatus($"{CustomMouseText.StatusRestoreWindowsDefaultFailed}: {ex.Message}", true);
        }
    }

    private void RefreshSummary()
    {
        var speed = (int)Math.Round(_pointerSpeed.Value);
        _speedValue.Text = $"{speed}/20";
        _previewValue.Text = $"{speed}/20";
        _buttonValue.Text = _swapButtons.IsChecked == true
            ? CustomMouseText.SwappedButtonsState
            : CustomMouseText.StandardButtonsState;
        var themeMode = _cursorTheme.SelectedIndex >= 0
            ? (CursorThemeMode)_cursorTheme.SelectedIndex
            : _plugin.Settings.CursorThemeMode;
        _themeValue.Text = ThemeText(themeMode);
    }

    private void SetStatus(string text, bool isError)
    {
        _status.Text = text;
        _status.Foreground = isError ? Brushes.IndianRed : Brushes.ForestGreen;
        _statusIcon.Text = isError ? "!" : "OK";
        _statusIcon.Foreground = _status.Foreground;
    }

    private static string ThemeText(CursorThemeMode mode) => mode switch
    {
        CursorThemeMode.Light => CustomMouseText.CursorThemeModeLight,
        CursorThemeMode.Dark => CustomMouseText.CursorThemeModeDark,
        CursorThemeMode.WindowsDefault => CustomMouseText.CursorThemeModeWindowsDefault,
        _ => CustomMouseText.CursorThemeModeAuto,
    };

    private static TextBlock ValueText() => new() { FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };

    private static Control Metric(string title, TextBlock value)
    {
        var panel = new StackPanel { Spacing = 3, MinWidth = 120 };
        panel.Children.Add(new TextBlock { Text = title, Foreground = Brushes.Gray, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(value);
        return panel;
    }

    private static Control Labelled(string label, Control editor, TextBlock value)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), RowDefinitions = new RowDefinitions("Auto,Auto"), RowSpacing = 6 };
        grid.Children.Add(new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap });
        Grid.SetColumn(value, 1);
        grid.Children.Add(value);
        Grid.SetRow(editor, 1);
        Grid.SetColumnSpan(editor, 2);
        grid.Children.Add(editor);
        return grid;
    }

    private static TextBlock Heading(string text) => new() { Text = text, FontSize = 16, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };

    private static Border Card(string title, string description, Control content)
    {
        var panel = new StackPanel { Spacing = 7 };
        panel.Children.Add(Heading(title));
        panel.Children.Add(new TextBlock { Text = description, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Gray });
        panel.Children.Add(content);
        return new Border
        {
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            BorderBrush = Brushes.Gray,
            Child = panel,
        };
    }

    private static Button ActionButton(string text, Func<Task> action, string automationId)
    {
        var button = new Button { Content = text, Padding = new Thickness(12, 7), MinWidth = 120 };
        AutomationProperties.SetAutomationId(button, automationId);
        ToolTip.SetTip(button, text);
        button.Click += async (_, _) => await action().ConfigureAwait(true);
        return button;
    }

    private static Button ActionButton(string text, Action action, string automationId)
    {
        var button = new Button { Content = text, Padding = new Thickness(12, 7), MinWidth = 120 };
        AutomationProperties.SetAutomationId(button, automationId);
        ToolTip.SetTip(button, text);
        button.Click += (_, _) => action();
        return button;
    }
}
