using System.Globalization;
using System.Resources;

namespace UniversalDeviceToolkit.Abstractions.Localization;

/// <summary>
/// ResourceManager-backed implementation with the shared UDT fallback policy.
/// </summary>
public sealed class ResourceManagerStringLocalizer : IStringLocalizer
{
    private readonly ResourceManager _resourceManager;

    public ResourceManagerStringLocalizer(ResourceManager resourceManager)
    {
        _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        CurrentCulture = CultureInfo.CurrentUICulture;
    }

    public string GetString(string key, string fallback = "") =>
        LocalizationCatalog.GetString(_resourceManager, key, fallback, CurrentCulture);

    public CultureInfo CurrentCulture { get; set; }
}
