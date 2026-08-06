using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;

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
    private bool _hydrating;

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
                ActionButton(CustomMouseText.ApplyCursorThemeNowButton, ApplyCursorThemeAsync),
                ActionButton(CustomMouseText.RestoreWindowsDefaultButton, RestoreWindowsDefaultAsync),
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
        actions.Children.Add(_status);
        actions.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                ActionButton(CustomMouseText.SyncFromWindowsButton, (Action)(() => SyncFromWindows())),
                ActionButton(CustomMouseText.ReloadButton, (Action)(() => SyncFromWindows())),
                ActionButton(CustomMouseText.ApplyToWindowsButton, ApplyToWindows),
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
        _pointerSpeed.ValueChanged += (_, _) =>
        {
            if (!_hydrating)
                _speedValue.Text = ((int)Math.Round(_pointerSpeed.Value)).ToString();
        };
        _swapButtons.IsCheckedChanged += (_, _) =>
        {
            if (!_hydrating)
                _ = ApplyToWindows();
        };
        _cursorTheme.SelectionChanged += async (_, _) =>
        {
            if (!_hydrating && _cursorTheme.SelectedIndex >= 0)
                await ApplyCursorThemeAsync();
        };
    }

    private void Hydrate()
    {
        _hydrating = true;
        _pointerSpeed.Value = _plugin.Settings.WindowsPointerSpeed;
        _swapButtons.IsChecked = _plugin.Settings.SwapButtons;
        _cursorTheme.SelectedIndex = (int)_plugin.Settings.CursorThemeMode;
        _hydrating = false;
        RefreshSummary();
        _status.Text = CustomMouseText.ProfileReady;
    }

    private async Task ApplyToWindows()
    {
        var speed = (int)Math.Round(_pointerSpeed.Value);
        var speedApplied = _plugin.SetWindowsPointerSpeed(speed);
        var swapApplied = _plugin.SetSwapButtons(_swapButtons.IsChecked == true);
        await _plugin.SaveSettingsAsync().ConfigureAwait(true);
        _status.Text = speedApplied && swapApplied
            ? CustomMouseText.StatusWindowsApplied
            : CustomMouseText.ApplyFailedPrefix + " " + CustomMouseText.StatusApplyPointerFail;
        RefreshSummary();
    }

    private void SyncFromWindows()
    {
        _plugin.ReloadSettingsFromSystem();
        Hydrate();
        _status.Text = CustomMouseText.StatusSyncedFromWindows;
    }

    private async Task ApplyCursorThemeAsync()
    {
        if (_cursorTheme.SelectedIndex < 0)
            return;

        var mode = (CursorThemeMode)_cursorTheme.SelectedIndex;
        var applied = await _plugin.SetCursorThemeModeAsync(mode).ConfigureAwait(true);
        _status.Text = applied ? CustomMouseText.StatusWindowsApplied : CustomMouseText.StatusCursorApplyFailed;
        RefreshSummary();
    }

    private async Task RestoreWindowsDefaultAsync()
    {
        var restored = await _plugin.RestoreWindowsDefaultCursorThemeAsync().ConfigureAwait(true);
        _status.Text = restored
            ? CustomMouseText.StatusWindowsDefaultRestored
            : CustomMouseText.StatusRestoreWindowsDefaultFailed;
        Hydrate();
    }

    private void RefreshSummary()
    {
        _speedValue.Text = _plugin.Settings.WindowsPointerSpeed.ToString();
        _previewValue.Text = _plugin.Settings.WindowsPointerSpeed.ToString();
        _buttonValue.Text = _plugin.Settings.SwapButtons ? CustomMouseText.SwappedButtonsState : CustomMouseText.StandardButtonsState;
        _themeValue.Text = ThemeText(_plugin.Settings.CursorThemeMode);
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

    private static Button ActionButton(string text, Func<Task> action)
    {
        var button = new Button { Content = text, Padding = new Thickness(12, 7), MinWidth = 120 };
        ToolTip.SetTip(button, text);
        button.Click += async (_, _) => await action().ConfigureAwait(true);
        return button;
    }

    private static Button ActionButton(string text, Action action)
    {
        var button = new Button { Content = text, Padding = new Thickness(12, 7), MinWidth = 120 };
        ToolTip.SetTip(button, text);
        button.Click += (_, _) => action();
        return button;
    }
}
