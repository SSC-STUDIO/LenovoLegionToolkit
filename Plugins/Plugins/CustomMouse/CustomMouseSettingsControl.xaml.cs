using System;
using System.Windows;
using System.Windows.Controls;

namespace LenovoLegionToolkit.Plugins.CustomMouse;

public partial class CustomMouseSettingsControl : UserControl
{
    private readonly CustomMousePlugin _plugin;

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

        _swapButtonsCheckBox = new CheckBox
        {
            Content = CustomMouseText.SwapButtonsLabel,
            Margin = new Thickness(0, 16, 0, 0)
        };

        _autoThemeCursorCheckBox = new CheckBox
        {
            Content = CustomMouseText.AutoThemeLabel,
            Margin = new Thickness(0, 12, 0, 0)
        };

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

        Grid.SetRow(_autoThemeCursorCheckBox, 3);
        root.Children.Add(_autoThemeCursorCheckBox);

        var hint = new TextBlock
        {
            Text = CustomMouseText.CursorHint,
            Margin = new Thickness(0, 12, 0, 0),
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(106, 106, 106))
        };
        Grid.SetRow(hint, 4);
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
        Grid.SetRow(actionPanel, 5);
        root.Children.Add(actionPanel);

        Grid.SetRow(_statusTextBlock, 6);
        root.Children.Add(_statusTextBlock);

        Content = root;
    }

    private void LoadCurrentValues()
    {
        if (_pointerSpeedSlider is null || _swapButtonsCheckBox is null || _autoThemeCursorCheckBox is null)
            return;

        _pointerSpeedSlider.Value = _plugin.Settings.WindowsPointerSpeed;
        _swapButtonsCheckBox.IsChecked = _plugin.Settings.SwapButtons;
        _autoThemeCursorCheckBox.IsChecked = _plugin.Settings.AutoThemeCursorStyle;
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pointerSpeedSlider is null || _swapButtonsCheckBox is null || _autoThemeCursorCheckBox is null || _statusTextBlock is null)
            return;

        var speed = (int)Math.Round(_pointerSpeedSlider.Value);
        var swapButtons = _swapButtonsCheckBox.IsChecked == true;

        if (!_plugin.SetWindowsPointerSpeed(speed))
        {
            _statusTextBlock.Text = CustomMouseText.StatusApplyPointerFail;
            return;
        }

        if (!_plugin.SetSwapButtons(swapButtons))
        {
            _statusTextBlock.Text = CustomMouseText.StatusApplySwapFail;
            return;
        }

        _plugin.SetAutoThemeCursorStyle(_autoThemeCursorCheckBox.IsChecked == true);
        await _plugin.SaveSettingsAsync().ConfigureAwait(true);
        _statusTextBlock.Text = CustomMouseText.StatusWindowsApplied;
    }

    private async void ApplyCursorThemeNowButton_Click(object sender, RoutedEventArgs e)
    {
        if (_autoThemeCursorCheckBox is null || _statusTextBlock is null)
            return;

        _plugin.SetAutoThemeCursorStyle(_autoThemeCursorCheckBox.IsChecked == true);
        await _plugin.SaveSettingsAsync().ConfigureAwait(true);

        var applied = await _plugin.ApplyCursorStyleForCurrentThemeAsync().ConfigureAwait(true);
        _statusTextBlock.Text = applied
            ? CustomMouseText.FormatCursorApplied(_plugin.Settings.LastAppliedTheme)
            : CustomMouseText.StatusCursorApplyFailed;
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_statusTextBlock is null)
            return;

        LoadCurrentValues();
        _statusTextBlock.Text = CustomMouseText.StatusReloaded;
    }
}
