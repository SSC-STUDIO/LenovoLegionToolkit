using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;

namespace UniversalDeviceToolkit.WPF.Pages;

public partial class PluginExtensionsPage
{
    private void UpdateAllPluginsUI()
    {
        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            ReconcileAvailableUpdatesWithInstalledVersions();

            // Merge online plugins and locally registered plugins.
            var allPluginsList = new List<IPlugin>();
            var pluginIds = new HashSet<string>();

            // First add locally installed plugins.
            var installedPlugins = _pluginManager.GetRegisteredPlugins().ToList();
            foreach (var plugin in installedPlugins)
            {
                allPluginsList.Add(plugin);
                pluginIds.Add(plugin.Id);
            }

            foreach (var installedPluginId in _pluginManager.GetInstalledPluginIds().Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (pluginIds.Contains(installedPluginId))
                    continue;

                var manifest = ResolvePluginManifestMetadata(installedPluginId) ?? new PluginManifest
                {
                    Id = installedPluginId,
                    Name = installedPluginId,
                    Description = string.Empty
                };

                if (string.IsNullOrWhiteSpace(manifest.Id))
                    manifest.Id = installedPluginId;
                if (string.IsNullOrWhiteSpace(manifest.Name))
                    manifest.Name = installedPluginId;

                allPluginsList.Add(new PluginManifestAdapter(manifest));
                pluginIds.Add(installedPluginId);
            }

            // Then add online plugins (using adapters), but skip already installed ones.
            if (_onlinePlugins.Count > 0)
            {
                foreach (var onlinePlugin in _onlinePlugins)
                {
                    if (!pluginIds.Contains(onlinePlugin.Id))
                        allPluginsList.Add(new PluginManifestAdapter(onlinePlugin));
                }
            }

            _allPlugins = allPluginsList;

            RebuildInstalledStateSnapshot(_allPlugins.Select(plugin => plugin.Id));
            UpdateBulkActionButtonsVisibility();

            // Apply current filters and search.
            ApplyFilters();

            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"PluginExtensionsPage: Found {_allPlugins.Count} total plugins");
                foreach (var plugin in _allPlugins)
                    Log.Instance.Trace($"  - {plugin.Id}: {plugin.Name} (System: {plugin.IsSystemPlugin}, Installed: {IsPluginInstalledForUi(plugin.Id)})");
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Trace($"Error updating plugins UI: {ex.Message}", ex);

            // Ensure "no plugins" message is shown even on error.
            if (_noPluginsMessage != null)
                _noPluginsMessage.Visibility = Visibility.Visible;
        }
        finally
        {
            if (Log.Instance.IsTraceEnabled)
            {
                var elapsed = Stopwatch.GetElapsedTime(startedAt);
                Log.Instance.Trace(
                    $"PluginExtensionsPage UI rebuild completed in {elapsed.TotalMilliseconds:0} ms. [plugins={_allPlugins.Count}, rows={_pluginViewModels.Count}]");
            }
        }
    }

    private bool TryGetAvailableUpdate(string pluginId, out PluginManifest? updatePlugin)
    {
        updatePlugin = _availableUpdates.FirstOrDefault(update =>
            string.Equals(update.Id, pluginId, StringComparison.OrdinalIgnoreCase));

        if (updatePlugin == null)
            return false;

        return IsAvailableUpdateNewerThanInstalled(pluginId, updatePlugin.Version);
    }

    private bool IsAvailableUpdateNewerThanInstalled(string pluginId, string? availableVersion)
    {
        if (string.IsNullOrWhiteSpace(pluginId) || !IsPluginInstalledForUi(pluginId))
            return false;

        if (string.IsNullOrWhiteSpace(availableVersion))
        {
            availableVersion = _onlinePlugins
                .FirstOrDefault(plugin => string.Equals(plugin.Id, pluginId, StringComparison.OrdinalIgnoreCase))
                ?.Version;
        }

        return PluginVersionParser.IsNewerThan(availableVersion, ResolveInstalledPluginVersion(pluginId));
    }

    private string? ResolveInstalledPluginVersion(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return null;

        var metadata = _pluginManager.GetPluginMetadata(pluginId);
        var manifest = TryReadInstalledPluginManifest(pluginId, metadata?.FilePath);
        if (!string.IsNullOrWhiteSpace(manifest?.Version))
            return manifest.Version;

        if (!string.IsNullOrWhiteSpace(metadata?.Version))
            return metadata.Version;

        return _recentInstalledVersions.TryGetValue(pluginId, out var recentVersion)
            ? recentVersion
            : null;
    }

    private bool IsPluginInstalledForUi(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        if (_installedStateSnapshot.TryGetValue(pluginId, out var installed))
            return installed;

        installed = ResolvePluginInstalledForUi(pluginId);
        _installedStateSnapshot[pluginId] = installed;
        return installed;
    }

    private void RebuildInstalledStateSnapshot(IEnumerable<string> pluginIds)
    {
        _installedStateSnapshot.Clear();
        foreach (var pluginId in pluginIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase))
            _installedStateSnapshot[pluginId] = ResolvePluginInstalledForUi(pluginId);
    }

    private bool ResolvePluginInstalledForUi(string pluginId)
    {
        if (_pluginManager.IsInstalled(pluginId))
            return true;

        try
        {
            var hasInstalledRecord = _pluginManager
                .GetInstalledPluginIds()
                .Contains(pluginId, StringComparer.OrdinalIgnoreCase);
            if (!hasInstalledRecord)
                return false;

            return PluginUiCapabilityResolver
                .ResolveFromInstalledManifest(pluginId)
                .HasAny;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to resolve UI installed state for {pluginId}: {ex.Message}", ex);

            return false;
        }
    }

    private void ReconcileAvailableUpdatesWithInstalledVersions()
    {
        if (_availableUpdates.Count == 0)
            return;

        var removedCount = _availableUpdates.RemoveAll(update =>
            !IsAvailableUpdateNewerThanInstalled(update.Id, update.Version));

        if (removedCount > 0 && Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"PluginExtensionsPage: removed {removedCount} stale plugin update marker(s)");
    }

    private void RemoveAvailableUpdate(string pluginId)
    {
        var removedCount = _availableUpdates.RemoveAll(update =>
            string.Equals(update.Id, pluginId, StringComparison.OrdinalIgnoreCase));

        if (removedCount > 0 && Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"PluginExtensionsPage: cleared update marker for {pluginId}");
    }

    private void UpdateSpecificPluginUI(string pluginId)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"UpdateSpecificPluginUI called for {pluginId}");
                Log.Instance.Trace($"  - IsInstalled for UI: {IsPluginInstalledForUi(pluginId)}");
                Log.Instance.Trace($"  - Available updates: {_availableUpdates.Count}");
                Log.Instance.Trace($"  - ViewModel count: {_pluginViewModels.Count}");
            }

            // Find corresponding ViewModel and update its status.
            var viewModel = _pluginViewModels.FirstOrDefault(vm => vm.PluginId == pluginId);
            if (viewModel != null)
            {
                var isInstalled = IsPluginInstalledForUi(pluginId);
                var updateAvailable = isInstalled && TryGetAvailableUpdate(pluginId, out _);

                if (Log.Instance.IsTraceEnabled)
                {
                    Log.Instance.Trace($"Found ViewModel for {pluginId}:");
                    Log.Instance.Trace($"  - Current IsInstalled: {viewModel.IsInstalled}");
                    Log.Instance.Trace($"  - New IsInstalled: {isInstalled}");
                    Log.Instance.Trace($"  - UpdateAvailable: {updateAvailable}");
                }

                viewModel.IsInstalled = isInstalled;
                viewModel.SetUpdateAvailable(updateAvailable);

                if (isInstalled)
                {
                    var plugin = _allPlugins.FirstOrDefault(p => p.Id == pluginId);
                    plugin = EnsureRegisteredPluginForUi(pluginId, isInstalled) ?? plugin;
                    var manifestMetadata = ResolvePluginManifestMetadata(pluginId);
                    var capabilities = ResolvePluginCapabilities(plugin, isInstalled, pluginId, manifestMetadata);
                    viewModel.SupportsConfiguration = capabilities.SupportsSettingsPage && _pluginManager.IsInstalled(pluginId);
                    viewModel.SupportsFeaturePage = capabilities.SupportsFeaturePage;
                    viewModel.SupportsOptimizationCategory = capabilities.SupportsOptimizationCategory;
                    viewModel.SupportsExecutableEntryPoint = TryResolvePluginExecutableForListing(pluginId);
                }
                else
                {
                    viewModel.SupportsConfiguration = false;
                    viewModel.SupportsFeaturePage = false;
                    viewModel.SupportsOptimizationCategory = false;
                    viewModel.SupportsExecutableEntryPoint = false;
                }

                if (Log.Instance.IsTraceEnabled)
                {
                    Log.Instance.Trace($"Updated plugin UI for {pluginId}: Installed={isInstalled}, UpdateAvailable={updateAvailable}");
                    Log.Instance.Trace($"  - ViewModel InstallButtonText after update: {viewModel.InstallButtonText}");
                }

                UpdateBulkActionButtonsVisibility();
            }
            else
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"ViewModel not found for {pluginId}, falling back to full UI update");

                UpdateAllPluginsUI();
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Trace($"Error updating specific plugin UI for {pluginId}: {ex.Message}", ex);
            UpdateAllPluginsUI();
        }
    }
}
