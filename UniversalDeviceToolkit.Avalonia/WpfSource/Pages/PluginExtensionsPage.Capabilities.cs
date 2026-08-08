using System;
using System.Reflection;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.WPF.Utils;

namespace UniversalDeviceToolkit.WPF.Pages;

public partial class PluginExtensionsPage
{
    private PluginUiCapabilities ResolvePluginCapabilities(
        IPlugin? plugin,
        bool isInstalled,
        string? pluginId = null,
        PluginManifest? manifest = null)
    {
        var manifestCapabilities = PluginUiCapabilityResolver.ResolveFromManifest(manifest);
        if (!isInstalled)
            return manifestCapabilities;

        pluginId = string.IsNullOrWhiteSpace(pluginId) ? plugin?.Id : pluginId;
        if (string.IsNullOrWhiteSpace(pluginId))
            return manifestCapabilities;

        return ResolveInstalledPluginCapabilities(
            plugin,
            manifestCapabilities,
            PluginUiCapabilityResolver.ResolveFromInstalledManifest(pluginId));
    }

    internal static PluginUiCapabilities ResolveInstalledPluginCapabilities(
        IPlugin? plugin,
        PluginUiCapabilities manifestCapabilities,
        PluginUiCapabilities installedManifestCapabilities)
    {
        var capabilities = manifestCapabilities.Merge(installedManifestCapabilities);

        if (plugin is not null and not PluginManifestAdapter)
            capabilities = capabilities.Merge(ResolveRuntimePluginCapabilities(plugin));

        return capabilities;
    }

    internal static PluginUiCapabilities ResolveRuntimePluginCapabilities(IPlugin plugin)
    {
        if (plugin is PluginManifestAdapter adapter)
            return PluginUiCapabilityResolver.ResolveFromManifest(adapter.Manifest);

        var supportsSettingsPage = false;
        var supportsFeaturePage = false;
        var supportsOptimizationCategory = false;

        try
        {
            if (plugin is PluginBase pluginBase)
            {
                var settingsPage = pluginBase.GetSettingsPage();
                supportsSettingsPage = settingsPage != null;

                var featureExtension = pluginBase.GetFeatureExtension();
                supportsFeaturePage = PluginPageWrapper.TryCreateHostedPluginPage(featureExtension, out _);

                var optimizationCategory = pluginBase.GetOptimizationCategory();
                supportsOptimizationCategory = optimizationCategory != null;
            }
            else
            {
                var pluginType = plugin.GetType();
                var getSettingsPage = pluginType.GetMethod("GetSettingsPage", BindingFlags.Public | BindingFlags.Instance);
                if (getSettingsPage != null)
                {
                    var settingsPage = getSettingsPage.Invoke(plugin, null);
                    supportsSettingsPage = settingsPage != null;
                }

                var getFeatureExtension = pluginType.GetMethod("GetFeatureExtension", BindingFlags.Public | BindingFlags.Instance);
                if (getFeatureExtension != null)
                {
                    var featureExtension = getFeatureExtension.Invoke(plugin, null);
                    supportsFeaturePage = PluginPageWrapper.TryCreateHostedPluginPage(featureExtension, out _);
                }

                if (plugin is IOptimizationCategoryProvider provider)
                {
                    supportsOptimizationCategory = provider.GetOptimizationCategory() != null;
                }
                else
                {
                    var getOptimizationCategory = pluginType.GetMethod("GetOptimizationCategory", BindingFlags.Public | BindingFlags.Instance);
                    if (getOptimizationCategory != null)
                    {
                        var optimizationCategory = getOptimizationCategory.Invoke(plugin, null);
                        supportsOptimizationCategory = optimizationCategory != null;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Failed to resolve plugin capability for {plugin.Id}", ex);
        }

        return new PluginUiCapabilities
        {
            SupportsSettingsPage = supportsSettingsPage,
            SupportsFeaturePage = supportsFeaturePage,
            SupportsOptimizationCategory = supportsOptimizationCategory,
        };
    }
}
