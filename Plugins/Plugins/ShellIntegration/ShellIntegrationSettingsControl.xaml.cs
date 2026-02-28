using System.Windows;
using System.Windows.Controls;

namespace LenovoLegionToolkit.Plugins.ShellIntegration;

public partial class ShellIntegrationSettingsControl : UserControl
{
    private readonly ShellIntegrationPlugin _plugin;

    public ShellIntegrationSettingsControl(ShellIntegrationPlugin plugin)
    {
        _plugin = plugin;
        TryInitializeComponent();
        RefreshStatus();
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
        _statusTextBlock = new TextBlock
        {
            Margin = new Thickness(0, 12, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };

        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var subtitle = new TextBlock
        {
            Text = ShellIntegrationText.Subtitle,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(subtitle, 0);
        root.Children.Add(subtitle);

        Grid.SetRow(_statusTextBlock, 1);
        root.Children.Add(_statusTextBlock);

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        var enableButton = new Button { Content = ShellIntegrationText.EnableButton, Width = 90 };
        enableButton.Click += EnableButton_Click;
        var disableButton = new Button { Content = ShellIntegrationText.DisableButton, Width = 90, Margin = new Thickness(8, 0, 0, 0) };
        disableButton.Click += DisableButton_Click;
        var styleButton = new Button { Content = ShellIntegrationText.OpenStyleShortButton, Width = 110, Margin = new Thickness(8, 0, 0, 0) };
        styleButton.Click += OpenStyleButton_Click;
        buttonPanel.Children.Add(enableButton);
        buttonPanel.Children.Add(disableButton);
        buttonPanel.Children.Add(styleButton);

        Grid.SetRow(buttonPanel, 2);
        root.Children.Add(buttonPanel);

        Content = root;
    }

    private void RefreshStatus(string? suffix = null)
    {
        if (_statusTextBlock is null)
            return;

        var installed = _plugin.IsShellInstalled();
        var path = _plugin.GetShellInstallPath() ?? ShellIntegrationText.NotFound;
        var prefix = installed ? ShellIntegrationText.StatusDetected : ShellIntegrationText.StatusNotDetected;

        _statusTextBlock.Text = $"{prefix}\n{ShellIntegrationText.PathLabel}: {path}";
        if (!string.IsNullOrWhiteSpace(suffix))
        {
            _statusTextBlock.Text += $"\n{suffix}";
        }
    }

    private async void EnableButton_Click(object sender, RoutedEventArgs e)
    {
        var success = await _plugin.EnableShellAsync().ConfigureAwait(true);
        RefreshStatus(success ? ShellIntegrationText.StatusEnableCompleted : ShellIntegrationText.StatusEnableFailed);
    }

    private async void DisableButton_Click(object sender, RoutedEventArgs e)
    {
        var success = await _plugin.DisableShellAsync().ConfigureAwait(true);
        RefreshStatus(success ? ShellIntegrationText.StatusDisableCompleted : ShellIntegrationText.StatusDisableFailed);
    }

    private void OpenStyleButton_Click(object sender, RoutedEventArgs e)
    {
        _plugin.OpenStyleSettingsWindow();
        RefreshStatus(ShellIntegrationText.StatusOpenedStyleSettings);
    }
}
