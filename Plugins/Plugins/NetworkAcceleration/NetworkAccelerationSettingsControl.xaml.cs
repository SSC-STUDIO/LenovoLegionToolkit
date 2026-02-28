using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

public partial class NetworkAccelerationSettingsControl : UserControl
{
    private readonly NetworkAccelerationPlugin _plugin;

    public NetworkAccelerationSettingsControl(NetworkAccelerationPlugin plugin)
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
        _autoOptimizeOnStartupCheckBox = new CheckBox
        {
            Content = NetworkAccelerationText.AutoOptimizeOnStartup,
            Margin = new Thickness(0, 0, 0, 8)
        };
        AutomationProperties.SetAutomationId(_autoOptimizeOnStartupCheckBox, "NetworkAcceleration_AutoOptimizeCheckBox");
        _resetWinsockCheckBox = new CheckBox
        {
            Content = NetworkAccelerationText.ResetWinsockOnOptimize,
            Margin = new Thickness(0, 0, 0, 8)
        };
        AutomationProperties.SetAutomationId(_resetWinsockCheckBox, "NetworkAcceleration_ResetWinsockCheckBox");
        _resetTcpIpCheckBox = new CheckBox
        {
            Content = NetworkAccelerationText.ResetTcpIpOnOptimize,
            Margin = new Thickness(0, 0, 0, 8)
        };
        AutomationProperties.SetAutomationId(_resetTcpIpCheckBox, "NetworkAcceleration_ResetTcpIpCheckBox");
        _statusTextBlock = new TextBlock
        {
            Margin = new Thickness(0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50))
        };
        AutomationProperties.SetAutomationId(_statusTextBlock, "NetworkAcceleration_SettingsStatusText");

        var checks = new StackPanel();
        checks.Children.Add(_autoOptimizeOnStartupCheckBox);
        checks.Children.Add(_resetWinsockCheckBox);
        checks.Children.Add(_resetTcpIpCheckBox);
        var saveButton = new Button { Content = NetworkAccelerationText.SaveSettingsButton, Width = 90, Margin = new Thickness(0, 12, 0, 0) };
        AutomationProperties.SetAutomationId(saveButton, "NetworkAcceleration_SaveSettingsButton");
        saveButton.Click += SaveButton_Click;

        var card = new Border
        {
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 210, 210)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14)
        };

        var cardStack = new StackPanel();
        cardStack.Children.Add(new TextBlock
        {
            Text = NetworkAccelerationText.SettingsTitle,
            FontSize = 16,
            FontWeight = FontWeights.Medium
        });
        cardStack.Children.Add(new TextBlock
        {
            Text = NetworkAccelerationText.SettingsDescription,
            Margin = new Thickness(0, 6, 0, 12),
            TextWrapping = TextWrapping.Wrap
        });
        cardStack.Children.Add(checks);
        cardStack.Children.Add(saveButton);
        cardStack.Children.Add(new Border
        {
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 210, 210)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 12, 0, 0),
            Child = _statusTextBlock
        });
        card.Child = cardStack;

        var root = new StackPanel { Margin = new Thickness(16) };
        AutomationProperties.SetAutomationId(root, "NetworkAcceleration_SettingsRoot");
        root.Children.Add(card);

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = root
        };
    }

    private void LoadCurrentValues()
    {
        if (_autoOptimizeOnStartupCheckBox is null || _resetWinsockCheckBox is null || _resetTcpIpCheckBox is null)
            return;

        _autoOptimizeOnStartupCheckBox.IsChecked = _plugin.Settings.AutoOptimizeOnStartup;
        _resetWinsockCheckBox.IsChecked = _plugin.Settings.ResetWinsockOnOptimize;
        _resetTcpIpCheckBox.IsChecked = _plugin.Settings.ResetTcpIpOnOptimize;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_autoOptimizeOnStartupCheckBox is null || _resetWinsockCheckBox is null || _resetTcpIpCheckBox is null || _statusTextBlock is null)
            return;

        _plugin.SetAutoOptimizeOnStartup(_autoOptimizeOnStartupCheckBox.IsChecked == true);
        _plugin.SetResetWinsockOnOptimize(_resetWinsockCheckBox.IsChecked == true);
        _plugin.SetResetTcpIpOnOptimize(_resetTcpIpCheckBox.IsChecked == true);

        await _plugin.SaveSettingsAsync().ConfigureAwait(true);
        _statusTextBlock.Text = NetworkAccelerationText.SettingsSaved;
    }
}
