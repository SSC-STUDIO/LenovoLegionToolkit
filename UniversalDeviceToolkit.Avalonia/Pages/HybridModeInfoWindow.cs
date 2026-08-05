using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;

namespace UniversalDeviceToolkit.Avalonia.Pages;

/// <summary>
/// Avalonia counterpart of WPF's ExtendedHybridModeInfoWindow. The visible
/// sections are derived from the host-reported options so unsupported modes are
/// never presented as available choices.
/// </summary>
public sealed class HybridModeInfoWindow : Window
{
    private readonly record struct Section(
        string State,
        string TitleKey,
        string MessageKey,
        string? DisclaimerKey = null);

    private static readonly Section[] Sections =
    [
        new("On", "ExtendedHybridModeInfoWindow_Hybrid_Title", "ExtendedHybridModeInfoWindow_Hybrid_Message"),
        new("OnIGPUOnly", "ExtendedHybridModeInfoWindow_IGPU_Title", "ExtendedHybridModeInfoWindow_IGPU_Message", "ExtendedHybridModeInfoWindow_IGPU_Disclaimer"),
        new("OnAuto", "ExtendedHybridModeInfoWindow_Auto_Title", "ExtendedHybridModeInfoWindow_Auto_Message"),
        new("Off", "ExtendedHybridModeInfoWindow_DGPU_Title", "ExtendedHybridModeInfoWindow_DGPU_Message", "ExtendedHybridModeInfoWindow_DGPU_Disclaimer"),
    ];

    public HybridModeInfoWindow(IReadOnlyList<DashboardStateOption> supportedOptions)
    {
        Title = Get("ExtendedHybridModeInfoWindow_Title", "Hybrid graphics information");
        Width = 550;
        MinWidth = 420;
        MaxWidth = 550;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var supported = new HashSet<string>(
            supportedOptions.Select(option => option.Value),
            StringComparer.OrdinalIgnoreCase);
        var sections = Sections.Where(section => supported.Contains(section.State)).ToArray();

        var content = new StackPanel
        {
            Spacing = 16,
            Margin = new Thickness(16),
            FlowDirection = LocalizationCatalog.IsRightToLeft(LocalizationRuntime.CurrentCulture)
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight,
        };

        foreach (var section in sections)
            content.Children.Add(CreateSection(section));

        var close = new Button
        {
            Content = Get("Close", "Close"),
            MinWidth = 120,
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(16, 8),
        };
        close.Click += (_, _) => Close();
        content.Children.Add(close);

        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = content,
        };
    }

    private static Control CreateSection(Section section)
    {
        var title = new LocalizedTextBlock
        {
            Text = Get(section.TitleKey, section.State),
            FontSize = 16,
            FontWeight = FontWeight.Medium,
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        var message = new LocalizedTextBlock
        {
            Text = Get(section.MessageKey, string.Empty),
            Foreground = new SolidColorBrush(Colors.Gray),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 4,
        };
        var stack = new StackPanel { Spacing = 4 };
        stack.Children.Add(title);
        stack.Children.Add(message);

        if (section.DisclaimerKey is not null)
        {
            stack.Children.Add(new LocalizedTextBlock
            {
                Text = Get(section.DisclaimerKey, string.Empty),
                FontWeight = FontWeight.Medium,
                Foreground = new SolidColorBrush(Colors.Gray),
                OverflowMode = LocalizedOverflowMode.Wrap,
                MaxLines = 4,
            });
        }

        return stack;
    }

    private static string Get(string key, string fallback) =>
        AvaloniaLocalization.GetString(key, fallback);
}
