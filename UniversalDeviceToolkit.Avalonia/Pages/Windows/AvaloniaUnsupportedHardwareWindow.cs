#if WINDOWS

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Pages.Windows;

/// <summary>
/// Safe startup decision for systems outside the verified hardware catalog.
/// The user can continue in basic mode or close the application before any
/// hardware-writing services are started.
/// </summary>
internal sealed class AvaloniaUnsupportedHardwareWindow : Window
{
    public AvaloniaUnsupportedHardwareWindow(MachineInformation machine)
    {
        Title = Get("UnsupportedWindow_Title", "Unsupported hardware");
        Width = 520;
        MinWidth = 420;
        MinHeight = 290;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;

        AutomationProperties.SetAutomationId(this, "AvaloniaUnsupportedHardwareWindow");
        AutomationProperties.SetName(this, Title);

        var title = new LocalizedTextBlock
        {
            Text = Title,
            FontSize = 22,
            FontWeight = FontWeight.Medium,
            Foreground = GetBrush("TextFillColorPrimaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        var message = new LocalizedTextBlock
        {
            Text = Get(
                "UnsupportedWindow_Message",
                "This device is not in the verified hardware catalog. Hardware-specific controls may be unavailable or behave unexpectedly."),
            Foreground = GetBrush("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 4,
        };
        var details = new LocalizedTextBlock
        {
            Text = $"{Get("DeviceInformationWindow_Vendor", "Vendor")}: {machine.Vendor}\n" +
                   $"{Get("DeviceInformationWindow_Model", "Model")}: {machine.Model}\n" +
                   $"{Get("DeviceInformationWindow_Bios", "BIOS")}: {machine.BiosVersion}",
            Foreground = GetBrush("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 4,
        };
        var continueButton = new Button
        {
            Content = Get("UnsupportedWindow_Continue", "Continue in basic mode"),
            MinWidth = 176,
        };
        AutomationProperties.SetAutomationId(continueButton, "AvaloniaUnsupportedHardwareContinueButton");
        AutomationProperties.SetName(continueButton, continueButton.Content?.ToString());
        continueButton.Click += (_, _) => Close(true);

        var exitButton = new Button
        {
            Content = Get("Exit", "Exit"),
            MinWidth = 88,
        };
        AutomationProperties.SetAutomationId(exitButton, "AvaloniaUnsupportedHardwareExitButton");
        AutomationProperties.SetName(exitButton, exitButton.Content?.ToString());
        exitButton.Click += (_, _) => Close(false);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { exitButton, continueButton },
        };
        Content = new Border
        {
            Padding = new global::Avalonia.Thickness(24),
            Background = GetBrush("CardBackgroundBrush"),
            Child = new StackPanel
            {
                Spacing = 16,
                FlowDirection = LocalizationCatalog.IsRightToLeft(LocalizationRuntime.CurrentCulture)
                    ? FlowDirection.RightToLeft
                    : FlowDirection.LeftToRight,
                Children = { title, message, details, buttons },
            },
        };
    }

    private static string Get(string key, string fallback) =>
        AvaloniaLocalization.GetString(key, fallback);

    private static IBrush GetBrush(string key) =>
        global::Avalonia.Application.Current?.TryFindResource(key, out var resource) == true
        && resource is IBrush brush
            ? brush
            : Brushes.Transparent;
}

#endif
