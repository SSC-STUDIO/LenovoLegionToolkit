namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Keeps the Avalonia shell's dynamic plugin navigation aligned with the WPF
/// host: only installed plugins that actually expose a feature page receive a
/// navigation entry.
/// </summary>
public static class PluginNavigationPolicy
{
    public static IReadOnlyList<PluginCatalogItem> GetVisiblePlugins(
        PluginCatalogState? catalog)
    {
        if (catalog is not { IsAvailable: true })
            return Array.Empty<PluginCatalogItem>();

        return catalog.Plugins
            .Where(plugin => plugin.IsInstalled
                && plugin.SupportsFeaturePage
                && !string.IsNullOrWhiteSpace(plugin.Id))
            .GroupBy(plugin => plugin.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }
}
