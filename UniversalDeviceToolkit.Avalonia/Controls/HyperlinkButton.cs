using System.Diagnostics;
using Avalonia;
using Avalonia.Interactivity;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// WPF-UI compatible hyperlink button: opens <see cref="NavigateUri"/> (http/https or
/// local path) in the default application when clicked.
/// </summary>
public class HyperlinkButton : Button
{
    /// <summary>Defines the <see cref="NavigateUri"/> property.</summary>
    public static readonly StyledProperty<string?> NavigateUriProperty =
        AvaloniaProperty.Register<HyperlinkButton, string?>(nameof(NavigateUri));

    /// <summary>
    /// Gets or sets the URI opened when the button is clicked.
    /// </summary>
    public string? NavigateUri
    {
        get => GetValue(NavigateUriProperty);
        set => SetValue(NavigateUriProperty, value);
    }

    public HyperlinkButton()
    {
        Click += OnHyperlinkButtonClick;
    }

    private void OnHyperlinkButtonClick(object? sender, RoutedEventArgs e)
    {
        var uri = NavigateUri;
        if (string.IsNullOrWhiteSpace(uri))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }
        catch
        {
            // Opening external links must never crash the application.
        }
    }
}
