using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace UniversalDeviceToolkit.Plugins.Shared;

/// <summary>
/// Helper class for WPF UI fallback patterns.
/// Provides standardized fallback UI construction when InitializeComponent fails.
/// </summary>
public static class WpfFallbackHelper
{
    /// <summary>
    /// Raised when InitializeComponent throws and the fallback builder runs.
    /// Hosts can subscribe to surface the real parse/load error in their diagnostics.
    /// </summary>
    public static event Action<Type, Exception>? ComponentInitializationFailed;

    /// <summary>
    /// Attempts to initialize a WPF component, falling back to manual construction if it fails.
    /// </summary>
    /// <typeparam name="T">The type of the control</typeparam>
    /// <param name="control">The control instance</param>
    /// <param name="fallbackBuilder">Action to build fallback UI if InitializeComponent fails</param>
    /// <returns>True if initialization succeeded, false if fallback was used</returns>
    public static bool TryInitializeComponent<T>(T control, Action fallbackBuilder) where T : FrameworkElement
    {
        ArgumentNullException.ThrowIfNull(fallbackBuilder);

        try
        {
            var method = typeof(T).GetMethod("InitializeComponent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(control, null);
                return true;
            }
        }
        catch (Exception ex)
        {
            // InitializeComponent failed, use fallback — but no longer silently.
            var error = ex is System.Reflection.TargetInvocationException { InnerException: not null } targetEx
                ? targetEx.InnerException!
                : ex;
            System.Diagnostics.Debug.WriteLine($"[WpfFallbackHelper] InitializeComponent failed for {typeof(T).FullName}: {error}");
            ComponentInitializationFailed?.Invoke(typeof(T), error);
        }

        fallbackBuilder();
        return false;
    }

    /// <summary>
    /// Builds a standardized fallback panel with message and optional details.
    /// </summary>
    /// <param name="message">Primary message to display</param>
    /// <param name="details">Optional details text</param>
    /// <returns>A configured StackPanel with the fallback content</returns>
    public static StackPanel BuildFallbackPanel(string message, string? details = null)
    {
        var panel = new StackPanel
        {
            Width = Constants.FallbackPanelWidth,
            Background = ResolveFallbackBrush("ApplicationBackgroundBrush", Brushes.White),
            Margin = new Thickness(Constants.DefaultSpacing)
        };

        var messageText = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Foreground = ResolveFallbackBrush("TextFillColorPrimaryBrush", Brushes.Black),
            Margin = new Thickness(0, 0, 0, Constants.DefaultSpacing)
        };
        panel.Children.Add(messageText);

        if (!string.IsNullOrEmpty(details))
        {
            var detailsText = new TextBlock
            {
                Text = details,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = ResolveFallbackBrush("TextFillColorSecondaryBrush", Brushes.Gray),
                Margin = new Thickness(0, 0, 0, Constants.DefaultSpacing)
            };
            panel.Children.Add(detailsText);
        }

        return panel;
    }

    private static Brush ResolveFallbackBrush(string resourceKey, Brush fallback)
    {
        return Application.Current?.TryFindResource(resourceKey) as Brush ?? fallback;
    }

    /// <summary>
    /// Creates a fallback error UI for display when a plugin fails to load.
    /// </summary>
    /// <param name="pluginName">Name of the plugin</param>
    /// <param name="errorMessage">Error message to display</param>
    /// <returns>A StackPanel with error information</returns>
    public static StackPanel CreateErrorFallback(string pluginName, string errorMessage)
    {
        return BuildFallbackPanel(
            $"{pluginName} Failed to Load",
            $"Error: {errorMessage}\n\nThe plugin encountered an error during initialization."
        );
    }
}
