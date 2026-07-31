using System.Globalization;
using UniversalDeviceToolkit.Abstractions.Localization;

namespace UniversalDeviceToolkit.Avalonia.Localization;

/// <summary>
/// A no-op IStringLocalizer that always returns the fallback text.
/// Useful when no .resx resource is available.
/// </summary>
public class FallbackStringLocalizer : IStringLocalizer
{
    /// <inheritdoc />
    public string GetString(string key, string fallback = "") => fallback;

    /// <inheritdoc />
    public CultureInfo CurrentCulture { get; set; } = CultureInfo.CurrentUICulture;
}
