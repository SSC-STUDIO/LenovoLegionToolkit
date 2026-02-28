using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LenovoLegionToolkit.Plugins.CustomMouse;

public partial class CustomMouseControl : UserControl
{
    private readonly CustomMousePlugin _plugin;

    public CustomMouseControl(CustomMousePlugin plugin)
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
        _dpiSlider = new Slider
        {
            Minimum = 100,
            Maximum = 16000,
            TickFrequency = 100,
            IsSnapToTickEnabled = true,
            Value = 1600
        };

        _pollingRateComboBox = new ComboBox();
        _pollingRateComboBox.Items.Add(new ComboBoxItem { Content = "125 Hz", Tag = "125" });
        _pollingRateComboBox.Items.Add(new ComboBoxItem { Content = "250 Hz", Tag = "250" });
        _pollingRateComboBox.Items.Add(new ComboBoxItem { Content = "500 Hz", Tag = "500" });
        _pollingRateComboBox.Items.Add(new ComboBoxItem { Content = "1000 Hz", Tag = "1000" });

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
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var subtitle = new TextBlock
        {
            Text = CustomMouseText.FeatureSubtitle,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        };
        Grid.SetRow(subtitle, 0);
        root.Children.Add(subtitle);

        var dpiPanel = new StackPanel();
        dpiPanel.Children.Add(new TextBlock { Text = CustomMouseText.DpiLabel, Margin = new Thickness(0, 0, 0, 4) });
        dpiPanel.Children.Add(_dpiSlider);
        Grid.SetRow(dpiPanel, 1);
        root.Children.Add(dpiPanel);

        var pollingPanel = new StackPanel { Margin = new Thickness(0, 12, 0, 0) };
        pollingPanel.Children.Add(new TextBlock { Text = CustomMouseText.PollingRateLabel, Margin = new Thickness(0, 0, 0, 4) });
        pollingPanel.Children.Add(_pollingRateComboBox);
        Grid.SetRow(pollingPanel, 2);
        root.Children.Add(pollingPanel);

        var actionPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        var applyButton = new Button { Content = CustomMouseText.ApplyButton, Width = 90 };
        applyButton.Click += ApplyButton_Click;
        var resetButton = new Button { Content = CustomMouseText.ResetButton, Width = 90, Margin = new Thickness(8, 0, 0, 0) };
        resetButton.Click += ResetButton_Click;
        actionPanel.Children.Add(applyButton);
        actionPanel.Children.Add(resetButton);
        Grid.SetRow(actionPanel, 3);
        root.Children.Add(actionPanel);

        Grid.SetRow(_statusTextBlock, 4);
        root.Children.Add(_statusTextBlock);

        Content = root;
    }

    private void LoadCurrentValues()
    {
        if (_dpiSlider is null || _pollingRateComboBox is null)
            return;

        _dpiSlider.Value = _plugin.Settings.Dpi;

        foreach (var item in _pollingRateComboBox.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag is string tag && int.TryParse(tag, out var value) && value == _plugin.Settings.PollingRate)
            {
                _pollingRateComboBox.SelectedItem = item;
                break;
            }
        }

        if (_pollingRateComboBox.SelectedItem == null && _pollingRateComboBox.Items.Count > 0)
        {
            _pollingRateComboBox.SelectedIndex = _pollingRateComboBox.Items.Count - 1;
        }
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        await ApplyCoreSettingsAsync().ConfigureAwait(true);
    }

    private async void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _plugin.OnInstalled();
        LoadCurrentValues();
        await _plugin.SaveSettingsAsync().ConfigureAwait(true);
        _statusTextBlock.Text = CustomMouseText.StatusResetDefaults;
    }

    private async Task ApplyCoreSettingsAsync()
    {
        var dpi = (int)Math.Round(_dpiSlider.Value);
        if (!_plugin.SetDpi(dpi))
        {
            _statusTextBlock.Text = CustomMouseText.StatusInvalidDpi;
            return;
        }

        var selected = _pollingRateComboBox.SelectedItem as ComboBoxItem;
        if (selected?.Tag is not string pollingTag || !int.TryParse(pollingTag, out var pollingRate))
        {
            _statusTextBlock.Text = CustomMouseText.StatusSelectValidPolling;
            return;
        }

        if (!_plugin.SetPollingRate(pollingRate))
        {
            _statusTextBlock.Text = CustomMouseText.StatusInvalidPolling;
            return;
        }

        await _plugin.SaveSettingsAsync().ConfigureAwait(true);
        _statusTextBlock.Text = CustomMouseText.StatusProfileSaved;
    }
}
