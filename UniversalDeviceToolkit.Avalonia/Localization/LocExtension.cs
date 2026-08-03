using Avalonia.Markup.Xaml;

namespace UniversalDeviceToolkit.Avalonia.Localization;

/// <summary>
/// Resolves a resource when a page is created. MainWindow recreates the active page after a
/// culture change, so every localized XAML value is refreshed without a custom binding engine.
/// </summary>
public sealed class LocExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;
    public string Fallback { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        AvaloniaLocalization.GetString(Key, Fallback);
}
