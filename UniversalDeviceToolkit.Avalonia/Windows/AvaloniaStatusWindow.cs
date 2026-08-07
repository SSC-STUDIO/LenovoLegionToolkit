using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;

namespace UniversalDeviceToolkit.Avalonia.Windows;

/// <summary>
/// Small borderless topmost popup with a status text area, shown near a given
/// screen point. Ported from the WPF StatusWindow for parity; consumers own the
/// text (power mode, sensors, battery, update) and the show/update/close flow.
/// </summary>
public sealed class AvaloniaStatusWindow : Window
{
    private const double PositionOffset = 8;

    private readonly TextBlock _text;

    public AvaloniaStatusWindow()
    {
        Title = AvaloniaLocalization.GetString("Window_Title", "Universal Device Toolkit");
        SystemDecorations = SystemDecorations.None;
        Topmost = true;
        CanResize = false;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.Manual;
        SizeToContent = SizeToContent.WidthAndHeight;
        Background = Brushes.Transparent;
        AutomationProperties.SetAutomationId(this, "AvaloniaStatusWindow");
        AutomationProperties.SetName(this, Title);

        _text = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 320,
            Foreground = GetBrush("TextFillColorPrimaryBrush"),
        };

        Content = new Border
        {
            Padding = new Thickness(14, 10),
            Background = GetBrush("CardBackgroundBrush"),
            BorderBrush = GetBrush("CardBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock
                    {
                        Text = Title,
                        FontSize = 12,
                        FontWeight = FontWeight.Medium,
                        Foreground = GetBrush("TextFillColorSecondaryBrush"),
                    },
                    _text,
                },
            },
        };
    }

    public void ShowAt(Point screenPoint, string text)
    {
        _text.Text = text;
        Position = new PixelPoint(
            (int)Math.Round(screenPoint.X + PositionOffset),
            (int)Math.Round(screenPoint.Y + PositionOffset));
        Show();
    }

    public void Update(string text) => _text.Text = text;

    private static IBrush GetBrush(string key) =>
        Application.Current?.TryFindResource(key, out var resource) == true
        && resource is IBrush brush
            ? brush
            : Brushes.Transparent;
}
