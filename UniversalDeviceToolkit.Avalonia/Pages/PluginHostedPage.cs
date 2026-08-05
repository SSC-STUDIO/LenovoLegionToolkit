using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

/// <summary>
/// Hosts a plugin entry page returned by the platform bridge. Avalonia-native
/// pages are embedded directly; WPF-only pages stay routable and show the
/// concrete compatibility reason instead of the old empty placeholder.
/// </summary>
public sealed class PluginHostedPage : UserControl
{
    private readonly IPlatformServices _platformServices;
    private readonly string _pluginId;
    private readonly Action _navigateBack;
    private readonly bool _isSettings;
    private readonly TextBlock _title = new();
    private readonly TextBlock _description = new();
    private readonly TextBlock _status = new();
    private readonly ContentControl _contentHost = new();
    private bool _loaded;

    public PluginHostedPage(
        IPlatformServices platformServices,
        string pluginId,
        Action navigateBack,
        bool isSettings = false)
    {
        _platformServices = platformServices;
        _pluginId = pluginId;
        _navigateBack = navigateBack;
        _isSettings = isSettings;

        var backButton = new Button
        {
            Content = AvaloniaLocalization.GetString("PluginPage_Back", "Back to Plugin Extensions"),
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 6),
        };
        AutomationProperties.SetAutomationId(backButton, "AvaloniaPluginPageBackButton");
        backButton.Click += (_, _) => _navigateBack();

        _title.FontSize = GetResource("FontSizePageTitle", 28d);
        _title.FontWeight = FontWeight.SemiBold;
        _title.Foreground = GetBrush("TextFillColorPrimaryBrush");
        _description.Foreground = GetBrush("TextFillColorSecondaryBrush");
        _description.TextWrapping = TextWrapping.Wrap;
        _status.Foreground = GetBrush("TextFillColorSecondaryBrush");
        _status.TextWrapping = TextWrapping.Wrap;

        var header = new StackPanel { Spacing = 8 };
        header.Children.Add(backButton);
        header.Children.Add(_title);
        header.Children.Add(_description);

        var statusCard = new Border
        {
            Background = GetBrush("CardBackgroundBrush"),
            BorderBrush = GetBrush("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = GetResource("CornerRadiusCard", new CornerRadius(8)),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 16, 0, 16),
            Child = _status,
        };

        var contentGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Margin = new Thickness(24),
        };
        Grid.SetRow(header, 0);
        Grid.SetRow(statusCard, 2);
        Grid.SetRow(_contentHost, 1);
        contentGrid.Children.Add(header);
        contentGrid.Children.Add(_contentHost);
        contentGrid.Children.Add(statusCard);

        Content = new ScrollViewer { Content = contentGrid };
        AutomationProperties.SetName(this, pluginId);
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_loaded)
            return;

        _loaded = true;
        PluginPageState state;
        try
        {
            state = _isSettings
                ? await _platformServices.GetPluginSettingsPageStateAsync(_pluginId)
                : await _platformServices.GetPluginPageStateAsync(_pluginId);
        }
        catch (Exception ex)
        {
            state = new PluginPageState(
                _pluginId,
                _pluginId,
                string.Empty,
                null,
                false,
                true,
                false,
                $"The plugin page could not be loaded: {ex.Message}");
        }
        _title.Text = string.IsNullOrWhiteSpace(state.Title) ? state.PluginId : state.Title;
        _description.Text = state.Description;
        _status.Text = state.StatusMessage;
        AutomationProperties.SetName(this, _title.Text);

        if (state.Content is Control control && state.IsAvaloniaPage)
        {
            _contentHost.Content = control;
            return;
        }

        _contentHost.Content = BuildCompatibilityState(state);
    }

    private Control BuildCompatibilityState(PluginPageState state)
    {
        var title = new LocalizedTextBlock
        {
            Text = state.HasFeaturePage
                ? AvaloniaLocalization.GetString(
                    _isSettings ? "PluginPage_WpfOnlySettingsTitle" : "PluginPage_WpfOnlyTitle",
                    _isSettings ? "Plugin settings require the WPF host" : "Plugin page requires the WPF host")
                : AvaloniaLocalization.GetString(
                    _isSettings ? "PluginPage_NoSettingsTitle" : "PluginPage_NoFeatureTitle",
                    _isSettings ? "No plugin settings page is available" : "No plugin feature page is available"),
            FontWeight = FontWeight.Medium,
            Foreground = GetBrush("TextFillColorPrimaryBrush"),
        };
        var message = new LocalizedTextBlock
        {
            Text = state.StatusMessage,
            Foreground = GetBrush("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 4,
        };
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(title);
        panel.Children.Add(message);
        return new Border
        {
            Background = GetBrush("CardBackgroundBrush"),
            BorderBrush = GetBrush("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = GetResource("CornerRadiusCard", new CornerRadius(8)),
            Padding = new Thickness(16),
            Child = panel,
        };
    }

    private IBrush GetBrush(string key)
    {
        return this.TryFindResource(key, out var value) && value is IBrush brush
            ? brush
            : new SolidColorBrush(Colors.Transparent);
    }

    private T GetResource<T>(string key, T fallback)
    {
        return this.TryFindResource(key, out var value) && value is T resource
            ? resource
            : fallback;
    }
}
