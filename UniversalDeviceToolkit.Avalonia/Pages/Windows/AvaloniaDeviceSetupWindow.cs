#if WINDOWS

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.DeviceSupport;

namespace UniversalDeviceToolkit.Avalonia.Pages.Windows;

internal sealed class AvaloniaDeviceSetupWindow : Window
{
    private readonly IReadOnlyList<DevicePackOption> _options;
    private readonly TaskCompletionSource<AvaloniaDeviceSetupDecision> _decision = new();
    private readonly ComboBox _packSelector = new();
    private readonly TextBlock _detail = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, IsVisible = false };
    private readonly Button _confirm = new();
    private readonly Button _skip = new();
    private bool _preparing;

    public AvaloniaDeviceSetupWindow(
        MachineInformation machine,
        DevicePack? recommended,
        bool isBasicMode,
        IReadOnlyList<DevicePack> selectable)
    {
        Title = Get("DeviceSetupWindow_Title", "Device setup");
        Width = 620;
        MinWidth = 480;
        MinHeight = 440;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        AutomationProperties.SetAutomationId(this, "AvaloniaDeviceSetupWindow");
        AutomationProperties.SetName(this, Title);
        _options = BuildOptions(recommended, selectable, isBasicMode);

        var title = CreateText(Title, 22, FontWeight.Medium, "TextFillColorPrimaryBrush");
        var summary = CreateText(
            recommended is null || isBasicMode
                ? Get("DeviceSetupWindow_BasicModeSummary", "This device will start in basic mode. Hardware controls stay hidden until a matching device pack is available.")
                : Get("DeviceSetupWindow_MatchingPackSummary", "A matching device pack was found. Confirm to apply it or select another profile."),
            14,
            FontWeight.Normal,
            "TextFillColorSecondaryBrush");
        var device = CreateText(
            $"{Get("DeviceInformationWindow_Vendor", "Vendor")}: {Display(machine.Vendor)}\n" +
            $"{Get("DeviceInformationWindow_Model", "Model")}: {Display(machine.Model)}\n" +
            $"{Get("DeviceInformationWindow_MachineType", "Machine type")}: {Display(machine.MachineType)}",
            13,
            FontWeight.Normal,
            "TextFillColorSecondaryBrush");

        _packSelector.ItemsSource = _options;
        _packSelector.SelectedItem = _options.FirstOrDefault(option => option.IsRecommended) ?? _options.FirstOrDefault();
        _packSelector.SelectionChanged += (_, _) => UpdateDetail();
        AutomationProperties.SetAutomationId(_packSelector, "AvaloniaDeviceSetupPackSelector");
        AutomationProperties.SetName(_packSelector, Get("DeviceSetupWindow_SelectPackLabel", "Device profile"));

        _confirm.Content = Get("DeviceSetupWindow_ConfirmButton", "Confirm");
        _confirm.MinWidth = 110;
        _confirm.Click += Confirm;
        AutomationProperties.SetAutomationId(_confirm, "AvaloniaDeviceSetupConfirmButton");
        _skip.Content = Get("DeviceSetupWindow_SkipButton", "Skip for now");
        _skip.MinWidth = 110;
        _skip.Click += (_, _) => CloseWith(AvaloniaDeviceSetupDecision.Deferred);
        AutomationProperties.SetAutomationId(_skip, "AvaloniaDeviceSetupSkipButton");

        var content = new StackPanel
        {
            Spacing = 14,
            Margin = new global::Avalonia.Thickness(24),
            FlowDirection = LocalizationCatalog.IsRightToLeft(LocalizationRuntime.CurrentCulture)
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight,
            Children =
            {
                title,
                summary,
                device,
                CreateText(Get("DeviceSetupWindow_SelectPackLabel", "Device profile (pack)"), 14, FontWeight.Medium, "TextFillColorPrimaryBrush"),
                _packSelector,
                _detail,
                _status,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { _skip, _confirm },
                },
            },
        };
        Content = new Border { Padding = new global::Avalonia.Thickness(0), Child = content };
        Closed += (_, _) => _decision.TrySetResult(AvaloniaDeviceSetupDecision.Deferred);
        UpdateDetail();
    }

    public Task<AvaloniaDeviceSetupDecision> Decision => _decision.Task;

    public void SetInstalling(string text)
    {
        _status.Text = text;
        _status.IsVisible = true;
    }

    public void SetFailed(string text)
    {
        _preparing = false;
        _confirm.IsEnabled = true;
        _skip.IsEnabled = true;
        _packSelector.IsEnabled = true;
        _status.Text = text;
        _status.IsVisible = true;
    }

    public void CompleteAndClose() => Close();

    private void Confirm(object? sender, global::Avalonia.Interactivity.RoutedEventArgs args)
    {
        if (_preparing || _decision.Task.IsCompleted)
            return;

        _preparing = true;
        _confirm.IsEnabled = false;
        _skip.IsEnabled = false;
        _packSelector.IsEnabled = false;
        _status.Text = Get("DeviceSetupWindow_Preparing", "Preparing device setup...");
        _status.IsVisible = true;

        var selected = _packSelector.SelectedItem as DevicePackOption ?? _options.FirstOrDefault();
        _decision.TrySetResult(selected is null
            ? AvaloniaDeviceSetupDecision.Deferred
            : new AvaloniaDeviceSetupDecision(true, selected.Id, !selected.IsHardware));
    }

    private void UpdateDetail()
    {
        var selected = _packSelector.SelectedItem as DevicePackOption;
        _detail.Text = selected?.IsHardware == true
            ? Get("DeviceSetupWindow_HardwarePackDetail", "Full hardware: power modes, sensors, fans, and device controls when supported.")
            : Get("DeviceSetupWindow_BasicPackDetail", "Basic profile: plugins, system optimization, language, and theme. Hardware controls stay hidden.");
    }

    private void CloseWith(AvaloniaDeviceSetupDecision decision)
    {
        _decision.TrySetResult(decision);
        Close();
    }

    private static IReadOnlyList<DevicePackOption> BuildOptions(
        DevicePack? recommended,
        IReadOnlyList<DevicePack> selectable,
        bool isBasicMode)
    {
        var options = new List<DevicePackOption>
        {
            new(
                CatalogDeviceSupportProvider.GenericBasicPackId,
                Get("DeviceSetupWindow_BasicModePackName", "Basic mode (plugins & optimization only)"),
                false,
                recommended is null || isBasicMode),
        };
        foreach (var pack in selectable
                     .Where(pack => !pack.Id.Equals(CatalogDeviceSupportProvider.GenericBasicPackId, StringComparison.OrdinalIgnoreCase))
                     .OrderByDescending(IsHardwarePack)
                     .ThenBy(pack => pack.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            var isRecommended = recommended?.Id.Equals(pack.Id, StringComparison.OrdinalIgnoreCase) == true;
            var label = isRecommended
                ? string.Format(Get("DeviceSetupWindow_RecommendedPackFormat", "{0} (recommended)"), pack.DisplayName)
                : pack.DisplayName;
            options.Add(new DevicePackOption(pack.Id, label, IsHardwarePack(pack), isRecommended));
        }
        return options;
    }

    private static bool IsHardwarePack(DevicePack pack) =>
        pack.EnabledFeatures.Any(feature => feature.Equals("lenovo-hardware-controls", StringComparison.OrdinalIgnoreCase));

    private static string Display(string? value) => string.IsNullOrWhiteSpace(value)
        ? Get("Unnamed", "Unknown")
        : value;

    private static TextBlock CreateText(string text, double fontSize, FontWeight weight, string brushKey) =>
        new()
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = weight,
            TextWrapping = TextWrapping.Wrap,
            Foreground = GetBrush(brushKey),
        };

    private static IBrush GetBrush(string key) =>
        global::Avalonia.Application.Current?.TryFindResource(key, out var resource) == true && resource is IBrush brush
            ? brush
            : Brushes.Transparent;

    private static string Get(string key, string fallback) => AvaloniaLocalization.GetString(key, fallback);

    private sealed record DevicePackOption(string Id, string Label, bool IsHardware, bool IsRecommended)
    {
        public override string ToString() => Label;
    }
}

internal readonly record struct AvaloniaDeviceSetupDecision(bool Confirmed, string? DevicePackId, bool IsBasicMode)
{
    public static AvaloniaDeviceSetupDecision Deferred => new(false, null, true);
}

#endif
