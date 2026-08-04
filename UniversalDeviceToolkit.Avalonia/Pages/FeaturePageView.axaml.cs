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

public partial class FeaturePageView : UserControl
{
    private readonly IPlatformServices _platformServices;
    private readonly FeaturePageDescriptor _descriptor;
    private readonly Action<string>? _actionRequested;
    private bool _isApplying;
    private FeaturePageState? _lastState;
    private bool _showCleanup;

    protected FeaturePageView(
        IPlatformServices platformServices,
        FeaturePageDescriptor descriptor,
        Action<string>? actionRequested = null)
    {
        _platformServices = platformServices;
        _descriptor = descriptor;
        _actionRequested = actionRequested;
        InitializeComponent();
        PageTitle.Text = descriptor.Title;
        PageDescription.Text = descriptor.Description;
        PageIcon.IconIdentifier = descriptor.IconIdentifier;
        StatusTitle.Text = AvaloniaLocalization.GetString("FeaturePage_StatusTitle", "Feature status");
        StatusMessage.Text = AvaloniaLocalization.GetString("FeaturePage_Loading", "Reading the current platform capability...");
        AutomationProperties.SetName(this, descriptor.Title);
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await RefreshStateAsync();
    }

    private async Task RefreshStateAsync()
    {
        try
        {
            _isApplying = true;
            var state = await _platformServices.GetFeaturePageStateAsync(_descriptor.RouteKey);
            _lastState = state;
            OptimizationToolbar.IsVisible = string.Equals(_descriptor.RouteKey, "WindowsOptimization", StringComparison.Ordinal);
            NetworkAccelerationButton.IsVisible = OptimizationToolbar.IsVisible;
            DriverDownloadButton.IsVisible = OptimizationToolbar.IsVisible;
            StatusTitle.Text = state.IsAvailable
                ? AvaloniaLocalization.GetString("FeaturePage_Available", "Available")
                : AvaloniaLocalization.GetString("FeaturePage_Unsupported", "Unavailable on this device");
            StatusMessage.Text = string.IsNullOrWhiteSpace(state.StatusMessage)
                ? _descriptor.UnsupportedReason
                : state.StatusMessage;
            StatusCard.Background = GetResource<IBrush>(state.IsAvailable ? "StatusSuccessBackgroundBrush" : "StatusInfoBackgroundBrush");
            StatusCard.BorderBrush = GetResource<IBrush>(state.IsAvailable ? "StatusSuccessBrush" : "StatusInfoBrush");

            RenderFeatureItems(state);
        }
        catch (Exception ex)
        {
            StatusTitle.Text = AvaloniaLocalization.GetString("FeaturePage_LoadFailed", "Unable to load feature state");
            StatusMessage.Text = ex.Message;
        }
        finally
        {
            _isApplying = false;
        }
    }

    private void RenderFeatureItems(FeaturePageState state)
    {
        FeatureItems.Items.Clear();
        var visibleActions = state.Actions.Where(action =>
            !_descriptor.RouteKey.Equals("WindowsOptimization", StringComparison.Ordinal)
            || (_showCleanup
                ? FeatureActionContract.IsCleanupAction(action.Key)
                    || action.Key is FeatureActionContract.CleanupScanActionKey
                    or FeatureActionContract.CleanupRunActionKey
                    or FeatureActionContract.CleanupClearActionKey
                : !FeatureActionContract.IsCleanupAction(action.Key)
                    && action.Key != FeatureActionContract.CleanupScanActionKey
                    && action.Key != FeatureActionContract.CleanupRunActionKey
                    && action.Key != FeatureActionContract.CleanupClearActionKey)).ToArray();

        string? lastCategory = null;
        foreach (var item in visibleActions)
        {
            if (!string.IsNullOrWhiteSpace(item.Category)
                && !string.Equals(lastCategory, item.Category, StringComparison.Ordinal))
            {
                FeatureItems.Items.Add(CreateCategoryHeading(item.Category));
                lastCategory = item.Category;
            }

            FeatureItems.Items.Add(CreateFeatureCard(item));
        }

        if (visibleActions.Length == 0)
            FeatureItems.Items.Add(CreateEmptyState());
        UpdateOptimizationCommands(state);
    }

    private void UpdateOptimizationCommands(FeaturePageState state)
    {
        if (!OptimizationToolbar.IsVisible)
            return;

        OptimizationCommands.IsVisible = !_showCleanup;
        CleanupCommands.IsVisible = _showCleanup;
        var cleanupSelected = state.Actions.Any(item => FeatureActionContract.IsCleanupAction(item.Key) && item.IsSelected);
        foreach (var button in CleanupCommands.Children.OfType<Button>())
        {
            var actionKey = button.Tag?.ToString();
            button.IsEnabled = actionKey switch
            {
                FeatureActionContract.CleanupClearActionKey => cleanupSelected,
                FeatureActionContract.CleanupScanActionKey => cleanupSelected,
                FeatureActionContract.CleanupRunActionKey => cleanupSelected,
                _ => true,
            };
        }
    }

    private void OptimizationModeButton_Click(object? sender, RoutedEventArgs e)
    {
        _showCleanup = false;
        if (_lastState is not null)
            RenderFeatureItems(_lastState);
    }

    private void CleanupModeButton_Click(object? sender, RoutedEventArgs e)
    {
        _showCleanup = true;
        if (_lastState is not null)
            RenderFeatureItems(_lastState);
    }

    private void NetworkAccelerationButton_Click(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var window = new Window
        {
            Title = AvaloniaLocalization.GetString("NetworkAccelerationPage_Title", "Network acceleration"),
            Width = 760,
            Height = 680,
            MinWidth = 620,
            MinHeight = 520,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new NetworkAccelerationPage(_platformServices),
        };
        window.Show(owner);
    }

    private void DriverDownloadButton_Click(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
            return;

        var window = new Window
        {
            Title = AvaloniaLocalization.GetString("WindowsOptimizationPage_Tab_DriverDownload", "Driver downloads"),
            Width = 860,
            Height = 720,
            MinWidth = 680,
            MinHeight = 560,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new DriverDownloadPage(_platformServices),
        };
        window.Show(owner);
    }

    private async void OptimizationCommandButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string actionKey } || _isApplying)
            return;

        _isApplying = true;
        try
        {
            if (await _platformServices.SetFeatureActionAsync(_descriptor.RouteKey, actionKey, true))
                await RefreshStateAsync();
        }
        finally
        {
            _isApplying = false;
        }
    }

    private Border CreateFeatureCard(FeatureActionItem item)
    {
        Control action;
        if (item.IsToggle)
        {
            var toggle = new CheckBox
            {
                IsChecked = item.IsSelected,
                IsEnabled = item.IsEnabled,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                MinWidth = 48,
            };
            AutomationProperties.SetAutomationId(toggle, $"Avalonia{_descriptor.RouteKey}_{item.Key}Toggle");
            AutomationProperties.SetName(toggle, item.Title);
            ToolTip.SetTip(toggle, item.Description);
            toggle.IsCheckedChanged += async (_, _) =>
            {
                if (_isApplying || toggle.IsChecked is not bool selected)
                    return;
                var accepted = await _platformServices.SetFeatureActionAsync(_descriptor.RouteKey, item.Key, selected);
                if (!accepted)
                    ToolTip.SetTip(toggle, item.Description + " " + item.Status);
                else
                    await RefreshStateAsync();
            };
            action = toggle;
        }
        else
        {
            var button = new Button
            {
                Content = item.Status,
                IsEnabled = item.IsEnabled,
                MinWidth = 120,
                VerticalAlignment = VerticalAlignment.Top,
            };
            AutomationProperties.SetAutomationId(button, $"Avalonia{_descriptor.RouteKey}_{item.Key}Action");
            AutomationProperties.SetName(button, item.Title);
            ToolTip.SetTip(button, item.Description);
            button.Click += async (_, _) =>
            {
                var accepted = await _platformServices.SetFeatureActionAsync(_descriptor.RouteKey, item.Key, true);
                if (!accepted)
                    ToolTip.SetTip(button, item.Description + " " + item.Status);
                else
                {
                    _actionRequested?.Invoke(item.Key);
                    await RefreshStateAsync();
                }
            };
            action = button;
        }

        var title = new LocalizedTextBlock
        {
            Text = item.Title,
            FontSize = GetResource<double>("FontSizeBody"),
            FontWeight = FontWeight.Medium,
            Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        var description = new LocalizedTextBlock
        {
            Text = item.Description,
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        };
        var copy = new StackPanel { Spacing = 4, MinWidth = 0 };
        copy.Children.Add(title);
        copy.Children.Add(description);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 14 };
        var icon = new NavigationIcon
        {
            IconIdentifier = item.IsToggle ? "ToggleRight24" : _descriptor.PrimaryActionIcon,
            FontSize = GetResource<double>("IconSizeLG"),
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            VerticalAlignment = VerticalAlignment.Top,
        };
        grid.Children.Add(icon);
        Grid.SetColumn(copy, 1);
        grid.Children.Add(copy);
        Grid.SetColumn(action, 2);
        grid.Children.Add(action);

        var card = new Border
        {
            Background = GetResource<IBrush>("CardBackgroundBrush"),
            BorderBrush = GetResource<IBrush>("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = GetResource<CornerRadius>("CornerRadiusCard"),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 8),
            Child = grid,
        };
        AutomationProperties.SetName(card, item.Title);
        return card;
    }

    private Border CreateEmptyState() => new()
    {
        Background = GetResource<IBrush>("CardBackgroundBrush"),
        BorderBrush = GetResource<IBrush>("CardBorderBrush"),
        BorderThickness = new Thickness(1),
        CornerRadius = GetResource<CornerRadius>("CornerRadiusCard"),
        Padding = new Thickness(16),
        Child = new LocalizedTextBlock
        {
            Text = AvaloniaLocalization.GetString("FeaturePage_NoActions", "No actions were reported by the platform adapter."),
            Foreground = GetResource<IBrush>("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        },
    };

    private LocalizedTextBlock CreateCategoryHeading(string category) => new()
    {
        Text = category,
        FontSize = GetResource<double>("FontSizeSubsection"),
        FontWeight = FontWeight.Medium,
        Foreground = GetResource<IBrush>("TextFillColorPrimaryBrush"),
        Margin = new Thickness(0, 10, 0, 2),
        OverflowMode = LocalizedOverflowMode.Wrap,
        MaxLines = 2,
    };

    private T GetResource<T>(object key)
    {
        if (this.TryFindResource(key, out var value) && value is T typedValue)
            return typedValue;

        if (typeof(T) == typeof(IBrush))
            return (T)(object)new SolidColorBrush(Colors.Transparent);
        if (typeof(T) == typeof(double))
            return (T)(object)14d;
        if (typeof(T) == typeof(CornerRadius))
            return (T)(object)new CornerRadius(8);
        throw new InvalidOperationException($"Missing Avalonia resource '{key}'.");
    }

    protected sealed record FeaturePageDescriptor(
        string RouteKey,
        string Title,
        string Description,
        string IconIdentifier,
        string UnsupportedReason,
        string PrimaryActionTitle,
        string PrimaryActionDescription,
        string PrimaryActionIcon,
        bool PrimaryActionEnabled = false);

}

public sealed class ActionsPage(IPlatformServices services) : AutomationPage(services);

public sealed class WindowsOptimizationPage(IPlatformServices services) : FeaturePageView(services, new(
    "WindowsOptimization",
    "System optimization",
    "Review Windows optimization actions and their current state.",
    "Gauge24",
    "Windows optimization actions require the Windows optimization adapter.",
    "Review optimization actions",
    "Apply or roll back supported Windows optimization actions from the shared optimization service.",
    "Gauge24"));

public sealed class PluginExtensionsPage : FeaturePageView
{
    public PluginExtensionsPage(IPlatformServices services, Action<string>? actionRequested = null)
        : base(services, new(
            "PluginExtensions",
            "Plugin Extensions",
            "Discover and manage optional plugin extensions.",
            "Apps24",
            "Plugin discovery and installation require the plugin service adapter.",
            "Review installed extensions",
            "Manage installed and registered extensions through the shared plugin manager.",
            "Apps24"), actionRequested)
    {
    }
}
