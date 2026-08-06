using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages.Windows;

/// <summary>
/// Host-neutral counterpart of the WPF optimization action details window.
/// The implementation catalog intentionally exposes the command/registry
/// surface without executing anything, so opening details is always safe.
/// </summary>
public sealed class ActionDetailsWindow : Window
{
    public ActionDetailsWindow(FeatureActionItem action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Title = Get("ActionDetailsWindow_Title", "Action details");
        Width = 800;
        Height = 600;
        MinWidth = 560;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        AutomationProperties.SetAutomationId(this, "AvaloniaActionDetailsWindow");
        AutomationProperties.SetName(this, Title);

        var details = AvaloniaActionDetailsCatalog.Get(action.Key);
        var content = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(20),
            FlowDirection = LocalizationCatalog.IsRightToLeft(LocalizationRuntime.CurrentCulture)
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight,
        };

        content.Children.Add(new LocalizedTextBlock
        {
            Text = action.Title,
            FontSize = 20,
            FontWeight = FontWeight.Medium,
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
            Foreground = ResolveBrush("TextFillColorPrimaryBrush"),
        });
        content.Children.Add(new LocalizedTextBlock
        {
            Text = action.Description,
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 4,
            Foreground = ResolveBrush("TextFillColorSecondaryBrush"),
        });

        var technical = new StackPanel { Spacing = 8 };
        technical.Children.Add(new LocalizedTextBlock
        {
            Text = Get("ActionDetailsWindow_TechnicalDetails", "Technical details"),
            FontSize = 16,
            FontWeight = FontWeight.Medium,
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
            Foreground = ResolveBrush("TextFillColorPrimaryBrush"),
        });

        var implementation = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            ColumnSpacing = 8,
        };
        implementation.Children.Add(new LocalizedTextBlock
        {
            Text = Get("ActionDetailsWindow_ImplementationType", "Implementation type"),
            FontWeight = FontWeight.Medium,
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        });
        var implementationValue = new LocalizedTextBlock
        {
            Text = details.ImplementationType,
            Foreground = ResolveBrush("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        Grid.SetColumn(implementationValue, 1);
        implementation.Children.Add(implementationValue);
        technical.Children.Add(implementation);

        var detailPanel = new Border
        {
            Padding = new Thickness(12),
            Background = ResolveBrush("SubtleFillColorTertiaryBrush"),
            BorderBrush = ResolveBrush("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = ResolveCornerRadius("CornerRadiusControl"),
            Child = new ScrollViewer
            {
                MaxHeight = 320,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = CreateDetailContent(details.Details),
            },
        };
        technical.Children.Add(detailPanel);

        content.Children.Add(new Border
        {
            Padding = new Thickness(14),
            Background = ResolveBrush("CardBackgroundBrush"),
            BorderBrush = ResolveBrush("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = ResolveCornerRadius("CornerRadiusCard"),
            Child = technical,
        });

        var close = new Button
        {
            Content = Get("ActionDetailsWindow_Close_Button", "Close"),
            MinWidth = 100,
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(16, 8),
        };
        AutomationProperties.SetAutomationId(close, "AvaloniaActionDetailsWindowCloseButton");
        AutomationProperties.SetName(close, close.Content?.ToString() ?? "Close");
        close.Click += (_, _) => Close();
        content.Children.Add(close);

        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = content,
        };
    }

    private static Control CreateDetailContent(IReadOnlyList<string> details)
    {
        var panel = new StackPanel { Spacing = 6 };
        if (details.Count == 0)
        {
            panel.Children.Add(new LocalizedTextBlock
            {
                Text = Get("ActionDetailsWindow_NoDetailsAvailable", "No technical details are available."),
                Foreground = new SolidColorBrush(Colors.Gray),
                OverflowMode = LocalizedOverflowMode.Wrap,
                MaxLines = 3,
            });
            return panel;
        }

        foreach (var detail in details)
        {
            panel.Children.Add(new TextBlock
            {
                Text = detail,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Foreground = ResolveBrush("TextFillColorPrimaryBrush"),
            });
        }

        return panel;
    }

    private static string Get(string key, string fallback) =>
        AvaloniaLocalization.GetString(key, fallback);

    private static IBrush ResolveBrush(string key) =>
        Application.Current?.Resources[key] is IBrush brush
            ? brush
            : new SolidColorBrush(Colors.Gray);

    private static CornerRadius ResolveCornerRadius(string key) =>
        Application.Current?.Resources[key] is CornerRadius radius
            ? radius
            : new CornerRadius(8);
}

public sealed record AvaloniaActionDetails(
    string ImplementationType,
    IReadOnlyList<string> Details);

/// <summary>
/// Mirrors the WPF action detail catalog for the built-in optimization keys.
/// Unknown/plugin keys still get a useful safe explanation instead of a blank
/// dialog.
/// </summary>
public static class AvaloniaActionDetailsCatalog
{
    public static AvaloniaActionDetails Get(string? actionKey)
    {
        var key = actionKey?.Trim() ?? string.Empty;
        if (key.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase))
            return new(Get("ActionDetailsWindow_CommandExecution", "Command execution"), Cleanup(key));
        if (key.StartsWith("services.", StringComparison.OrdinalIgnoreCase))
            return new(Get("ActionDetailsWindow_ServiceManagement", "Service management"), Services(key));
        if (key.StartsWith("explorer.", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("performance.", StringComparison.OrdinalIgnoreCase))
            return new(Get("ActionDetailsWindow_RegistryModification", "Registry modification"), Registry(key));
        if (key.StartsWith("network.", StringComparison.OrdinalIgnoreCase))
            return new(Get("ActionDetailsWindow_CommandExecution", "Command execution"),
                [Get("ActionDetailsWindow_NetworkFlushDNS", "Flush DNS cache"),
                 Get("ActionDetailsWindow_NetworkResetWinsock", "Reset Winsock"),
                 Get("ActionDetailsWindow_NetworkResetTCPIP", "Reset TCP/IP")]);

        return new(Get("ActionDetailsWindow_UnknownImplementation", "Unknown implementation"),
            [Get("ActionDetailsWindow_NoDetailsAvailable", "No technical details are available.")]);
    }

    private static IReadOnlyList<string> Cleanup(string key) => key.ToLowerInvariant() switch
    {
        "cleanup.browsercache" =>
        ["del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\INetCache\\*\" >nul 2>&1",
         "del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\INetCookies\\*\" >nul 2>&1"],
        "cleanup.thumbnailcache" =>
        ["del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\Explorer\\thumbcache_*.db\" >nul 2>&1",
         "del /f /s /q \"%LocalAppData%\\Local\\D3DSCache\\*\" >nul 2>&1"],
        "cleanup.windowsupdate" =>
        ["del /f /s /q \"%SystemRoot%\\SoftwareDistribution\\Download\\*\" >nul 2>&1",
         "del /f /s /q \"%SystemRoot%\\SoftwareDistribution\\DeliveryOptimization\\*\" >nul 2>&1"],
        "cleanup.tempfiles" =>
        ["del /f /s /q \"%SystemRoot%\\Temp\\*\" >nul 2>&1",
         "del /f /s /q \"%SystemDrive%\\Windows\\Temp\\*\" >nul 2>&1",
         "del /f /s /q \"%TEMP%\\*\" >nul 2>&1"],
        "cleanup.recyclebin" => ["rd /s /q \"%SystemDrive%\\$Recycle.bin\" >nul 2>&1"],
        "cleanup.componentstore" =>
        ["dism /Online /Cleanup-Image /StartComponentCleanup /ResetBase",
         "del /f /s /q \"%SystemRoot%\\WinSxS\\Temp\\*\" >nul 2>&1"],
        "cleanup.registry" => [Get("ActionDetailsWindow_CleanupRegistry", "Registry cleanup")],
        _ => [],
    };

    private static IReadOnlyList<string> Services(string key) => key.ToLowerInvariant() switch
    {
        "services.diagnostics" =>
        ["Service name: DiagTrack",
         "Service name: diagnosticshub.standardcollector.service",
         "Service name: DoSvc",
         "Action: Disable and stop service"],
        "services.sysmain" => ["Service name: SysMain (Superfetch)", "Action: Disable and stop service"],
        "services.search" => ["Service name: WSearch (Windows Search)", "Action: Disable and stop service"],
        _ => [],
    };

    private static IReadOnlyList<string> Registry(string key) => key.ToLowerInvariant() switch
    {
        "explorer.taskbar" =>
        [@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
         "  - TaskbarDa: 0 (Disable taskbar animations)",
         "  - TaskbarAnimations: 0 (Disable taskbar animation effects)"],
        "explorer.responsiveness" =>
        [@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
         "  - DesktopProcess: 1 (Optimize desktop process)",
         "  - DisablePreviewDesktop: 1 (Disable desktop preview)"],
        "performance.telemetry" =>
        [@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection",
         "  - AllowTelemetry: 0 (Disable telemetry)"],
        "performance.memory" =>
        [@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
         "  - DisablePagingExecutive: 1 (Disable paging executive)",
         "  - LargeSystemCache: 0 (Optimize system cache)"],
        "performance.multimedia" =>
        [@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Multimedia\SystemProfile",
         "  - SystemResponsiveness: 0 (Optimize multimedia responsiveness)",
         "  - NetworkThrottlingIndex: 4294967295 (Disable network throttling)"],
        _ => [],
    };

    private static string Get(string key, string fallback) =>
        AvaloniaLocalization.GetString(key, fallback);
}
