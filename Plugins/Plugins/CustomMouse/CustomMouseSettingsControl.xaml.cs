using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace LenovoLegionToolkit.Plugins.CustomMouse;

public partial class CustomMouseSettingsControl : UserControl
{
    private readonly CustomMousePlugin _plugin;
    private static CultureInfo Culture => CustomMouseText.Culture;

    public CustomMouseSettingsControl(CustomMousePlugin plugin)
    {
        _plugin = plugin;
        TryInitializeComponent();
        LoadCurrentValues();
    }

    private void TryInitializeComponent()
    {
        try
        {
            InitializeComponent();
        }
        catch
        {
            BuildFallbackUi();
        }
    }

    private void BuildFallbackUi()
    {
        _pointerSpeedSlider = new Slider
        {
            Minimum = 1,
            Maximum = 20,
            TickFrequency = 1,
            IsSnapToTickEnabled = true
        };
        _pointerSpeedSlider.ValueChanged += SettingsInputChanged;

        _swapButtonsCheckBox = new CheckBox
        {
            Content = CustomMouseText.SwapButtonsLabel,
            Margin = new Thickness(0, 16, 0, 0)
        };
        _swapButtonsCheckBox.Checked += SettingsInputChanged;
        _swapButtonsCheckBox.Unchecked += SettingsInputChanged;

        _cursorThemeModeComboBox = new ComboBox { Margin = new Thickness(0, 12, 0, 0) };
        _cursorThemeModeComboBox.Items.Add(new ComboBoxItem { Content = CustomMouseText.CursorThemeModeAuto, Tag = "Auto" });
        _cursorThemeModeComboBox.Items.Add(new ComboBoxItem { Content = CustomMouseText.CursorThemeModeLight, Tag = "Light" });
        _cursorThemeModeComboBox.Items.Add(new ComboBoxItem { Content = CustomMouseText.CursorThemeModeDark, Tag = "Dark" });
        _cursorThemeModeComboBox.SelectionChanged += CursorThemeModeComboBox_SelectionChanged;

        _statusTextBlock = new TextBlock
        {
            Margin = new Thickness(0, 12, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50))
        };

        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var subtitle = new TextBlock
        {
            Text = CustomMouseText.SettingsSubtitle,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        };
        Grid.SetRow(subtitle, 0);
        root.Children.Add(subtitle);

        var speedPanel = new StackPanel();
        speedPanel.Children.Add(new TextBlock { Text = CustomMouseText.PointerSpeedLabel, Margin = new Thickness(0, 0, 0, 4) });
        speedPanel.Children.Add(_pointerSpeedSlider);
        Grid.SetRow(speedPanel, 1);
        root.Children.Add(speedPanel);

        Grid.SetRow(_swapButtonsCheckBox, 2);
        root.Children.Add(_swapButtonsCheckBox);

        var themeLabel = new TextBlock
        {
            Text = CustomMouseText.CursorThemeModeLabel,
            Margin = new Thickness(0, 12, 0, 4)
        };
        Grid.SetRow(themeLabel, 3);
        root.Children.Add(themeLabel);

        Grid.SetRow(_cursorThemeModeComboBox, 4);
        root.Children.Add(_cursorThemeModeComboBox);

        var hint = new TextBlock
        {
            Text = CustomMouseText.CursorHint,
            Margin = new Thickness(0, 12, 0, 0),
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(106, 106, 106))
        };
        Grid.SetRow(hint, 5);
        root.Children.Add(hint);

        var actionPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        var applyButton = new Button { Content = CustomMouseText.ApplyToWindowsButton, Width = 130 };
        applyButton.Click += ApplyButton_Click;
        var applyCursorButton = new Button { Content = CustomMouseText.ApplyCursorThemeNowButton, Width = 170, Margin = new Thickness(8, 0, 0, 0) };
        applyCursorButton.Click += ApplyCursorThemeNowButton_Click;
        var reloadButton = new Button { Content = CustomMouseText.ReloadButton, Width = 90, Margin = new Thickness(8, 0, 0, 0) };
        reloadButton.Click += ReloadButton_Click;
        actionPanel.Children.Add(applyButton);
        actionPanel.Children.Add(applyCursorButton);
        actionPanel.Children.Add(reloadButton);
        Grid.SetRow(actionPanel, 6);
        root.Children.Add(actionPanel);

        Grid.SetRow(_statusTextBlock, 7);
        root.Children.Add(_statusTextBlock);

        Content = root;
    }

    private void LoadCurrentValues()
    {
        if (_pointerSpeedSlider is null || _swapButtonsCheckBox is null)
            return;

        _pointerSpeedSlider.Value = _plugin.Settings.WindowsPointerSpeed;
        _swapButtonsCheckBox.IsChecked = _plugin.Settings.SwapButtons;
        LoadCursorThemeMode();
        UpdateSummaryCards();
        UpdatePointerSpeedValueLabel();
    }

    private void LoadCursorThemeMode()
    {
        if (_cursorThemeModeComboBox is null)
            return;

        var modeTag = _plugin.Settings.CursorThemeMode switch
        {
            CursorThemeMode.Light => "Light",
            CursorThemeMode.Dark => "Dark",
            _ => "Auto"
        };

        foreach (var item in _cursorThemeModeComboBox.Items)
        {
            if (item is ComboBoxItem comboItem && comboItem.Tag as string == modeTag)
            {
                _cursorThemeModeComboBox.SelectedItem = comboItem;
                break;
            }
        }
    }

    private void SettingsInputChanged(object sender, RoutedEventArgs e)
    {
        UpdateSummaryCards();
        UpdatePointerSpeedValueLabel();
    }

    private void UpdatePointerSpeedValueLabel()
    {
        if (_pointerSpeedValueLabel is null || _pointerSpeedSlider is null)
            return;

        _pointerSpeedValueLabel.Text = string.Format(Culture, "{0}/20", (int)Math.Round(_pointerSpeedSlider.Value));
    }

    private async void CursorThemeModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_cursorThemeModeComboBox?.SelectedItem is not ComboBoxItem selected)
            return;

        var mode = (selected.Tag as string) switch
        {
            "Light" => CursorThemeMode.Light,
            "Dark" => CursorThemeMode.Dark,
            _ => CursorThemeMode.Auto
        };

        var applied = await _plugin.SetCursorThemeModeAsync(mode).ConfigureAwait(true);
        await _plugin.SaveSettingsAsync().ConfigureAwait(true);

        if (applied)
        {
            var modeText = mode switch
            {
                CursorThemeMode.Light => CustomMouseText.CursorThemeModeLight,
                CursorThemeMode.Dark => CustomMouseText.CursorThemeModeDark,
                _ => CustomMouseText.CursorThemeModeAuto
            };
            SetStatus(string.Format(CustomMouseText.CursorThemeModeApplied, modeText), false);
        }
        else
        {
            SetStatus(CustomMouseText.StatusCursorApplyFailed, true);
        }

        UpdateSummaryCards();
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pointerSpeedSlider is null || _swapButtonsCheckBox is null || _statusTextBlock is null)
            return;

        var speed = (int)Math.Round(_pointerSpeedSlider.Value);
        var swapButtons = _swapButtonsCheckBox.IsChecked == true;

        if (!_plugin.SetWindowsPointerSpeed(speed))
        {
            SetStatus(CustomMouseText.StatusApplyPointerFail, true);
            UpdateSummaryCards();
            return;
        }

        if (!_plugin.SetSwapButtons(swapButtons))
        {
            SetStatus(CustomMouseText.StatusApplySwapFail, true);
            UpdateSummaryCards();
            return;
        }

        await _plugin.SaveSettingsAsync().ConfigureAwait(true);
        SetStatus(CustomMouseText.StatusWindowsApplied, false);
        UpdateSummaryCards();
    }

    private async void ApplyCursorThemeNowButton_Click(object sender, RoutedEventArgs e)
    {
        if (_statusTextBlock is null)
            return;

        var applied = await _plugin.ApplyCursorStyleForCurrentThemeAsync().ConfigureAwait(true);
        SetStatus(
            applied ? CustomMouseText.FormatCursorApplied(_plugin.Settings.LastAppliedTheme) : CustomMouseText.StatusCursorApplyFailed,
            !applied);
        UpdateSummaryCards();
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_statusTextBlock is null)
            return;

        LoadCurrentValues();
        SetStatus(CustomMouseText.StatusReloaded, false);
        UpdateSummaryCards();
    }

    private void SetStatus(string text, bool isError)
    {
        if (_statusTextBlock is null)
            return;

        _statusTextBlock.Text = text;
        _statusTextBlock.Foreground = isError
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(196, 43, 28))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 123, 90));

        if (_statusIcon is not null)
        {
            _statusIcon.Symbol = isError
                ? Wpf.Ui.Common.SymbolRegular.ErrorCircle24
                : Wpf.Ui.Common.SymbolRegular.CheckmarkCircle24;
            _statusIcon.Foreground = _statusTextBlock.Foreground;
        }
    }

    private void UpdateSummaryCards()
    {
        if (_pointerPreviewValueTextBlock != null)
        {
            var speed = _pointerSpeedSlider is null ? _plugin.Settings.WindowsPointerSpeed : (int)Math.Round(_pointerSpeedSlider.Value);
            _pointerPreviewValueTextBlock.Text = string.Format(Culture, "{0}/20", speed);
        }

        if (_buttonLayoutValueTextBlock != null)
            _buttonLayoutValueTextBlock.Text = _swapButtonsCheckBox?.IsChecked == true
                ? CustomMouseText.SwappedButtonsState
                : CustomMouseText.StandardButtonsState;

        if (_cursorThemeValueTextBlock != null)
        {
            var mode = GetSelectedCursorThemeMode();
            _cursorThemeValueTextBlock.Text = mode switch
            {
                CursorThemeMode.Light => CustomMouseText.CursorThemeModeLight,
                CursorThemeMode.Dark => CustomMouseText.CursorThemeModeDark,
                _ => CustomMouseText.CursorThemeModeAuto
            };
        }
    }

    private CursorThemeMode GetSelectedCursorThemeMode()
    {
        if (_cursorThemeModeComboBox?.SelectedItem is ComboBoxItem selected)
        {
            return (selected.Tag as string) switch
            {
                "Light" => CursorThemeMode.Light,
                "Dark" => CursorThemeMode.Dark,
                _ => CursorThemeMode.Auto
            };
        }

        return _plugin.Settings.CursorThemeMode;
    }
}
