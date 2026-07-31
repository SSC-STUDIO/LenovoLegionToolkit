using System.Globalization;
using System.Resources;
using UniversalDeviceToolkit.Abstractions.Localization;

namespace UniversalDeviceToolkit.Avalonia.Localization;

/// <summary>
/// IStringLocalizer implementation backed by a .resx ResourceManager.
/// </summary>
public class ResxStringLocalizer : IStringLocalizer
{
    private readonly ResourceManager _resourceManager;

    public ResxStringLocalizer(ResourceManager resourceManager)
    {
        _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
    }

    /// <inheritdoc />
    public string GetString(string key, string fallback = "")
    {
        try
        {
            return _resourceManager.GetString(key, CurrentCulture) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    /// <inheritdoc />
    public CultureInfo CurrentCulture { get; set; } = CultureInfo.CurrentUICulture;
}
