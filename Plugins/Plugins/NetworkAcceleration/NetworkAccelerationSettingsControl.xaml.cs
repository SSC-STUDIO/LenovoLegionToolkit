using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

public partial class NetworkAccelerationSettingsControl : UserControl
{
    private readonly NetworkAccelerationPlugin _plugin;

    public NetworkAccelerationSettingsControl(NetworkAccelerationPlugin plugin)
    {
        _plugin = plugin;
        TryInitializeComponent();
        LoadCurrentValues();
        UpdateSummary();
        SetStatus(NetworkAccelerationText.SettingsSummaryDescription, false);
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
        _autoOptimizeOnStartupCheckBox.Checked += SettingsCheckBox_Changed;
        _autoOptimizeOnStartupCheckBox.Unchecked += SettingsCheckBox_Changed;
        AutomationProperties.SetAutomationId(_autoOptimizeOnStartupCheckBox, "NetworkAcceleration_AutoOptimizeCheckBox");

        _resetWinsockCheckBox = new CheckBox
        {
            Content = NetworkAccelerationText.ResetWinsockOnOptimize,
            Margin = new Thickness(0, 0, 0, 8)
        };
        _resetWinsockCheckBox.Checked += SettingsCheckBox_Changed;
        _resetWinsockCheckBox.Unchecked += SettingsCheckBox_Changed;
        AutomationProperties.SetAutomationId(_resetWinsockCheckBox, "NetworkAcceleration_ResetWinsockCheckBox");

        _resetTcpIpCheckBox = new CheckBox
        {
            Content = NetworkAccelerationText.ResetTcpIpOnOptimize
        };
        _resetTcpIpCheckBox.Checked += SettingsCheckBox_Changed;
        _resetTcpIpCheckBox.Unchecked += SettingsCheckBox_Changed;
        AutomationProperties.SetAutomationId(_resetTcpIpCheckBox, "NetworkAcceleration_ResetTcpIpCheckBox");

        _statusTextBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap
        };
        AutomationProperties.SetAutomationId(_statusTextBlock, "NetworkAcceleration_SettingsStatusText");

        _modeSummaryTextBlock = new TextBlock();
        _startupSummaryTextBlock = new TextBlock();
        _winsockSummaryTextBlock = new TextBlock();
        _tcpSummaryTextBlock = new TextBlock();

        var root = new StackPanel { Margin = new Thickness(16) };
        AutomationProperties.SetAutomationId(root, "NetworkAcceleration_SettingsRoot");
        root.Children.Add(_autoOptimizeOnStartupCheckBox);
        root.Children.Add(_resetWinsockCheckBox);
        root.Children.Add(_resetTcpIpCheckBox);

        var saveButton = new Button
        {
            Content = NetworkAccelerationText.SaveSettingsButton,
            Width = 120,
            Margin = new Thickness(0, 12, 0, 0)
        };
        AutomationProperties.SetAutomationId(saveButton, "NetworkAcceleration_SaveSettingsButton");
        saveButton.Click += SaveButton_Click;
        root.Children.Add(saveButton);
        root.Children.Add(_statusTextBlock);

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

    private void SettingsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        if (_modeSummaryTextBlock != null)
            _modeSummaryTextBlock.Text = GetModeLabel(_plugin.Settings.PreferredMode);

        if (_startupSummaryTextBlock != null)
            _startupSummaryTextBlock.Text = GetToggleLabel(_autoOptimizeOnStartupCheckBox?.IsChecked == true);

        if (_winsockSummaryTextBlock != null)
            _winsockSummaryTextBlock.Text = GetToggleLabel(_resetWinsockCheckBox?.IsChecked == true);

        if (_tcpSummaryTextBlock != null)
            _tcpSummaryTextBlock.Text = GetToggleLabel(_resetTcpIpCheckBox?.IsChecked == true);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_autoOptimizeOnStartupCheckBox is null || _resetWinsockCheckBox is null || _resetTcpIpCheckBox is null)
            return;

        _plugin.SetAutoOptimizeOnStartup(_autoOptimizeOnStartupCheckBox.IsChecked == true);
        _plugin.SetResetWinsockOnOptimize(_resetWinsockCheckBox.IsChecked == true);
        _plugin.SetResetTcpIpOnOptimize(_resetTcpIpCheckBox.IsChecked == true);

        await _plugin.SaveSettingsAsync().ConfigureAwait(true);
        UpdateSummary();
        SetStatus(NetworkAccelerationText.SettingsSaved, false);
    }

    private void SetStatus(string text, bool isError)
    {
        if (_statusTextBlock is null)
            return;

        _statusTextBlock.Text = text;
        _statusTextBlock.Foreground = isError
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC42B1C"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0F7B5A"));
    }

    private static string GetModeLabel(NetworkAccelerationMode mode)
    {
        return mode switch
        {
            NetworkAccelerationMode.Gaming => NetworkAccelerationText.ModeGaming,
            NetworkAccelerationMode.Streaming => NetworkAccelerationText.ModeStreaming,
            _ => NetworkAccelerationText.ModeBalanced
        };
    }

    private static string GetToggleLabel(bool enabled) => enabled
        ? NetworkAccelerationText.StateEnabled
        : NetworkAccelerationText.StateDisabled;
}
