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

    protected FeaturePageView(IPlatformServices platformServices, FeaturePageDescriptor descriptor)
    {
        _platformServices = platformServices;
        _descriptor = descriptor;
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
        try
        {
            var supported = await _platformServices.IsSupportedLegionMachineAsync();
            var groups = await _platformServices.GetFeatureGroupsAsync();
            StatusTitle.Text = supported
                ? AvaloniaLocalization.GetString("FeaturePage_Available", "Available")
                : AvaloniaLocalization.GetString("FeaturePage_Unsupported", "Unavailable on this device");
            StatusMessage.Text = supported
                ? AvaloniaLocalization.GetString("FeaturePage_AvailableMessage", "The platform adapter reported this feature as available.")
                : _descriptor.UnsupportedReason;
            StatusCard.Background = GetResource<IBrush>(supported ? "StatusSuccessBackgroundBrush" : "StatusInfoBackgroundBrush");
            StatusCard.BorderBrush = GetResource<IBrush>(supported ? "StatusSuccessBrush" : "StatusInfoBrush");

            FeatureItems.Items.Clear();
            foreach (var item in BuildItems(groups, supported))
                FeatureItems.Items.Add(CreateFeatureCard(item));
        }
        catch (Exception ex)
        {
            StatusTitle.Text = AvaloniaLocalization.GetString("FeaturePage_LoadFailed", "Unable to load feature state");
            StatusMessage.Text = ex.Message;
        }
    }

    private IEnumerable<FeatureItem> BuildItems(IReadOnlyList<FeatureGroupItem> groups, bool supported)
    {
        yield return new FeatureItem(
            _descriptor.PrimaryActionTitle,
            _descriptor.PrimaryActionDescription,
            _descriptor.PrimaryActionIcon,
            _descriptor.PrimaryActionTitle,
            supported && _descriptor.PrimaryActionEnabled);

        foreach (var group in groups.Take(4))
        {
            yield return new FeatureItem(
                group.Title,
                group.Description,
                _descriptor.PrimaryActionIcon,
                group.Status,
                false);
        }
    }

    private Border CreateFeatureCard(FeatureItem item)
    {
        var action = new Button
        {
            Content = item.ActionLabel,
            IsEnabled = item.IsActionEnabled,
            MinWidth = 120,
            VerticalAlignment = VerticalAlignment.Top,
        };
        AutomationProperties.SetAutomationId(action, $"Avalonia{_descriptor.RouteKey}_{Sanitize(item.ActionLabel)}Button");
        AutomationProperties.SetName(action, item.ActionLabel);
        ToolTip.SetTip(action, item.Description);

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
            IconIdentifier = item.IconIdentifier,
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

    private static string Sanitize(string value) =>
        string.Concat(value.Where(char.IsLetterOrDigit));

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

    private sealed record FeatureItem(
        string Title,
        string Description,
        string IconIdentifier,
        string ActionLabel,
        bool IsActionEnabled);
}

public sealed class KeyboardBacklightPage(IPlatformServices services) : FeaturePageView(services, new(
    "Keyboard",
    "Keyboard",
    "Configure keyboard backlight and keyboard-specific controls.",
    "Keyboard24",
    "Keyboard hardware controls require a compatible Windows device adapter.",
    "Open keyboard controls",
    "Review supported lighting modes and keyboard hardware state.",
    "Keyboard24"));

public sealed class ActionsPage(IPlatformServices services) : FeaturePageView(services, new(
    "Actions",
    "Actions",
    "Review supported device actions and hardware workflows.",
    "Rocket24",
    "Hardware action execution is not exposed by this platform adapter.",
    "Review available actions",
    "Actions are shown for inspection and remain non-destructive until a device adapter is available.",
    "Rocket24"));

public sealed class MacroPage(IPlatformServices services) : FeaturePageView(services, new(
    "Macro",
    "Macro",
    "Create and manage device macros.",
    "ReceiptPlay24",
    "Macro execution requires the Windows input and device services.",
    "Open macro workspace",
    "Macro definitions can be reviewed without sending input to the host.",
    "ReceiptPlay24"));

public sealed class WindowsOptimizationPage(IPlatformServices services) : FeaturePageView(services, new(
    "WindowsOptimization",
    "System optimization",
    "Review Windows optimization actions and their current state.",
    "Gauge24",
    "Windows optimization actions require the Windows optimization adapter.",
    "Review optimization actions",
    "Actions are read-only in this migration surface; no system changes are executed.",
    "Gauge24"));

public sealed class PluginExtensionsPage(IPlatformServices services) : FeaturePageView(services, new(
    "PluginExtensions",
    "Plugin Extensions",
    "Discover and manage optional plugin extensions.",
    "Apps24",
    "Plugin discovery and installation require the plugin service adapter.",
    "Review installed extensions",
    "Plugin actions remain non-destructive until the host adapter is available.",
    "Apps24"));
