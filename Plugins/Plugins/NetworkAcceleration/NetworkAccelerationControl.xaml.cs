using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

public partial class NetworkAccelerationControl : UserControl
{
    private readonly NetworkAccelerationPlugin _plugin;

    public NetworkAccelerationControl(NetworkAccelerationPlugin plugin)
    {
        _plugin = plugin;
        TryInitializeComponent();
        LoadCurrentMode();
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
        _modeComboBox = new ComboBox();
        AutomationProperties.SetAutomationId(_modeComboBox, "NetworkAcceleration_ModeComboBox");
        _modeComboBox.Items.Add(new ComboBoxItem { Content = NetworkAccelerationText.ModeBalanced, Tag = "Balanced" });
        _modeComboBox.Items.Add(new ComboBoxItem { Content = NetworkAccelerationText.ModeGaming, Tag = "Gaming" });
        _modeComboBox.Items.Add(new ComboBoxItem { Content = NetworkAccelerationText.ModeStreaming, Tag = "Streaming" });

        _statusTextBlock = new TextBlock
        {
            Margin = new Thickness(0, 0, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50))
        };
        AutomationProperties.SetAutomationId(_statusTextBlock, "NetworkAcceleration_StatusText");

        var quickCard = new Border
        {
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 210, 210)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var quickStack = new StackPanel();
        quickStack.Children.Add(new TextBlock
        {
            Text = NetworkAccelerationText.QuickActionsTitle,
            FontSize = 16,
            FontWeight = FontWeights.Medium
        });
        quickStack.Children.Add(new TextBlock
        {
            Text = NetworkAccelerationText.QuickActionsDescription,
            Margin = new Thickness(0, 6, 0, 12),
            TextWrapping = TextWrapping.Wrap
        });
        var actionPanel = new WrapPanel();
        var optimizeButton = new Button { Content = NetworkAccelerationText.RunQuickOptimizationButton, Width = 120 };
        AutomationProperties.SetAutomationId(optimizeButton, "NetworkAcceleration_QuickOptimizeButton");
        optimizeButton.Click += QuickOptimizeButton_Click;
        var resetButton = new Button { Content = NetworkAccelerationText.ResetNetworkStackButton, Width = 110, Margin = new Thickness(8, 0, 0, 0) };
        AutomationProperties.SetAutomationId(resetButton, "NetworkAcceleration_ResetStackButton");
        resetButton.Click += ResetStackButton_Click;
        actionPanel.Children.Add(optimizeButton);
        actionPanel.Children.Add(resetButton);
        quickStack.Children.Add(actionPanel);
        quickCard.Child = quickStack;

        var modeCard = new Border
        {
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 210, 210)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14)
        };
        var modeStack = new StackPanel();
        modeStack.Children.Add(new TextBlock
        {
            Text = NetworkAccelerationText.PreferredModeTitle,
            FontSize = 16,
            FontWeight = FontWeights.Medium
        });
        _modeComboBox.Width = 220;
        _modeComboBox.Margin = new Thickness(0, 10, 0, 0);
        modeStack.Children.Add(_modeComboBox);
        var saveButton = new Button { Content = NetworkAccelerationText.SaveModeButton, Width = 100, Margin = new Thickness(0, 12, 0, 0) };
        AutomationProperties.SetAutomationId(saveButton, "NetworkAcceleration_SaveModeButton");
        saveButton.Click += SaveModeButton_Click;
        modeStack.Children.Add(saveButton);
        modeStack.Children.Add(new Border
        {
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 210, 210)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 12, 0, 0),
            Child = _statusTextBlock
        });
        modeCard.Child = modeStack;

        var root = new StackPanel { Margin = new Thickness(16) };
        AutomationProperties.SetAutomationId(root, "NetworkAcceleration_FeatureRoot");
        root.Children.Add(quickCard);
        root.Children.Add(modeCard);

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = root
        };
    }

    private void LoadCurrentMode()
    {
        if (_modeComboBox is null)
            return;

        var modeValue = _plugin.Settings.PreferredMode.ToString();
        foreach (var item in _modeComboBox.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag as string == modeValue)
            {
                _modeComboBox.SelectedItem = item;
                break;
            }
        }

        if (_modeComboBox.SelectedItem == null && _modeComboBox.Items.Count > 0)
        {
            _modeComboBox.SelectedIndex = 0;
        }
    }

    private async void QuickOptimizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_statusTextBlock is null)
            return;

        var success = await _plugin.RunQuickOptimizationAsync().ConfigureAwait(true);
        _statusTextBlock.Text = success
            ? NetworkAccelerationText.StatusQuickOptimizationCompleted
            : NetworkAccelerationText.StatusQuickOptimizationFailed;
    }

    private async void ResetStackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_statusTextBlock is null)
            return;

        var success = await _plugin.ResetNetworkStackAsync().ConfigureAwait(true);
        _statusTextBlock.Text = success
            ? NetworkAccelerationText.StatusResetCompleted
            : NetworkAccelerationText.StatusResetFailed;
    }

    private async void SaveModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_modeComboBox is null || _statusTextBlock is null)
            return;

        if (_modeComboBox.SelectedItem is not ComboBoxItem combo || combo.Tag is not string modeText)
        {
            _statusTextBlock.Text = NetworkAccelerationText.StatusSelectValidMode;
            return;
        }

        if (!Enum.TryParse(modeText, true, out NetworkAccelerationMode mode))
        {
            _statusTextBlock.Text = NetworkAccelerationText.StatusSelectValidMode;
            return;
        }

        _plugin.SetPreferredMode(mode);
        await _plugin.SaveSettingsAsync().ConfigureAwait(true);
        _statusTextBlock.Text = NetworkAccelerationText.StatusModeSaved;
    }
}
