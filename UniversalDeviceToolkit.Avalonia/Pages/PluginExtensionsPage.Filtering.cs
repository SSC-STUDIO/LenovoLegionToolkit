using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Avalonia.Resources;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class PluginExtensionsPage
{
    private void ApplyFilters()
    {
        var filteredPlugins = _allPlugins.AsEnumerable();

        // Apply filter
        filteredPlugins = _currentFilter switch
        {
            "Installed" => filteredPlugins.Where(p => IsPluginInstalledForUi(p.Id)),
            "NotInstalled" => filteredPlugins.Where(p => !IsPluginInstalledForUi(p.Id)),
            _ => filteredPlugins
        };

        // Apply search
        if (!string.IsNullOrWhiteSpace(_currentSearchText))
        {
            var searchLower = _currentSearchText.ToLowerInvariant();
            filteredPlugins = filteredPlugins.Where(p =>
            {
                var manifest = ResolvePluginManifestForDisplay(p);
                var metadata = CreatePluginDisplayMetadata(p, manifest);
                var culture = Resource.Culture ?? CultureInfo.CurrentUICulture;
                return metadata.GetDisplayName(culture).ToLowerInvariant().Contains(searchLower) ||
                       metadata.GetDisplayDescription(culture).ToLowerInvariant().Contains(searchLower) ||
                       p.Id.ToLowerInvariant().Contains(searchLower) ||
                       metadata.GetDisplayTags(culture).Any(tag => tag.ToLowerInvariant().Contains(searchLower));
            });
        }

        UpdatePluginsList(filteredPlugins.ToList());
    }

    private void UpdatePluginsList(List<IPlugin> plugins)
    {
        if (_pluginsListBox == null) return;

        // Remove duplicates: deduplicate by plugin ID
        var uniquePlugins = plugins.GroupBy(p => p.Id).Select(g => g.First()).ToList();

        // Create current plugin ID set for quick lookup
        var currentPluginIds = new HashSet<string>(uniquePlugins.Select(p => p.Id));

        // Remove ViewModels for plugins that no longer exist
        for (int i = _pluginViewModels.Count - 1; i >= 0; i--)
        {
            var viewModel = _pluginViewModels[i];
            if (!currentPluginIds.Contains(viewModel.PluginId))
            {
                _pluginViewModels.RemoveAt(i);
            }
        }

        var isLoading = _loadingIndicator?.IsVisible;
        var hasVisiblePlugins = uniquePlugins.Any();
        var hasAnyPlugins = _allPlugins.Any();

        if (_noPluginsMessage != null)
            _noPluginsMessage.IsVisible = !(isLoading ?? false) && !hasVisiblePlugins && !hasAnyPlugins ? true : false;

        if (_noResultsStackPanel != null)
            _noResultsStackPanel.IsVisible = !(isLoading ?? false) && !hasVisiblePlugins && hasAnyPlugins ? true : false;

        foreach (var plugin in uniquePlugins)
        {
            try
            {
                var isInstalled = IsPluginInstalledForUi(plugin.Id);

                if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"UpdatePluginsList: Plugin {plugin.Id} - UI installed check returned {isInstalled}");
                }

                PluginManifest? updatePlugin = null;
                var updateAvailable = isInstalled && TryGetAvailableUpdate(plugin.Id, out updatePlugin);

                // Get changelog info
                var changelog = updateAvailable ? (updatePlugin?.Changelog ?? string.Empty) : string.Empty;
                var releaseDate = updateAvailable ? FormatReleaseDate(updatePlugin?.ReleaseDate ?? string.Empty) : string.Empty;
                var newVersion = updateAvailable ? (updatePlugin?.Version ?? string.Empty) : string.Empty;

                // Get version information
                var metadata = _pluginManager.GetPluginMetadata(plugin.Id);
                var onlinePlugin = _onlinePlugins.FirstOrDefault(op => op.Id == plugin.Id);
                var iconBackground = updatePlugin?.IconBackground ?? onlinePlugin?.IconBackground ?? string.Empty;

                string version = "1.0.0";
                if (isInstalled)
                {
                    var installedVersion = ResolveInstalledPluginVersion(plugin.Id);
                    if (!string.IsNullOrWhiteSpace(installedVersion))
                        version = installedVersion;
                }
                else if (metadata != null && !string.IsNullOrWhiteSpace(metadata.Version))
                    version = metadata.Version;
                else if (!string.IsNullOrWhiteSpace(newVersion))
                    version = newVersion;
                else if (onlinePlugin != null && !string.IsNullOrWhiteSpace(onlinePlugin.Version))
                    version = onlinePlugin.Version;
                else if (metadata != null && !string.IsNullOrWhiteSpace(metadata.Version))
                    version = metadata.Version;

                // Determine if plugin is local based on its installation path
                // Simplified logic: plugins directly in 'plugins' folder are remote, others are local
                bool isLocal = false;
                if (metadata?.FilePath != null)
                {
                    var pluginsDir = GetPluginsDirectory();
                    var pluginDir = Path.GetDirectoryName(metadata.FilePath);
                    var parentDir = Path.GetDirectoryName(pluginDir);

                    isLocal = !string.Equals(parentDir, pluginsDir, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    // If not installed, base it on whether it's available online
                    isLocal = onlinePlugin == null;
                }

                // Prefer already-in-memory store/online manifests; disk reads are cached.
                var installedManifest = isInstalled ? TryReadInstalledPluginManifest(plugin.Id, metadata?.FilePath) : null;
                var manifestMetadata = installedManifest ?? updatePlugin ?? onlinePlugin;
                // List rebuild must NOT force plugin reload (ScanAndLoad) - that freezes the UI.
                var resolvedPlugin = GetRegisteredPluginForUi(plugin.Id, reloadIfMissing: false) ?? plugin;
                var capabilities = ResolvePluginCapabilities(resolvedPlugin, isInstalled, plugin.Id, manifestMetadata);
                // Existence-only for list badges; Authenticode runs on launch only.
                var supportsExecutableEntryPoint = isInstalled && TryResolvePluginExecutableForListing(plugin.Id);
                var localizedName = GetPluginLocalizedName(plugin, manifestMetadata);
                var localizedDescription = GetPluginLocalizedDescription(plugin, manifestMetadata);
                var localizedTags = GetPluginLocalizedTags(plugin, manifestMetadata);
                var detailedDescription = GetPluginDetailedDescription(manifestMetadata);
                var usageGuide = GetPluginUsageGuide(manifestMetadata);

                // Determine location
                string location = string.Empty;
                if (isInstalled)
                {
                    if (plugin.IsSystemPlugin || !capabilities.SupportsFeaturePage)
                    {
                        location = Resource.PluginExtensionsPage_LocationSystem;
                    }
                    else
                    {
                        location = Resource.PluginExtensionsPage_LocationSidebar;
                    }
                }

                // Find existing ViewModel, update if exists, otherwise create new one
                var existingViewModel = _pluginViewModels.FirstOrDefault(vm => vm.PluginId == plugin.Id);

                if (existingViewModel != null)
                {
                    // Update existing ViewModel
                    existingViewModel.Name = localizedName;
                    existingViewModel.Description = localizedDescription;
                    existingViewModel.Tags = localizedTags;
                    existingViewModel.IsInstalled = isInstalled;
                    existingViewModel.SetUpdateAvailable(updateAvailable);
                    existingViewModel.Version = $"v{version}";
                    existingViewModel.IsLocal = isLocal;
                    existingViewModel.Location = location;
                    existingViewModel.NewVersion = newVersion;
                    existingViewModel.ReleaseDate = releaseDate;
                    existingViewModel.Changelog = changelog;
                    existingViewModel.Author = metadata?.Author ?? string.Empty;
                    existingViewModel.DetailedDescription = detailedDescription;
                    existingViewModel.UsageGuide = usageGuide;
                    existingViewModel.SetIconBackgroundFromStore(iconBackground);

                    if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                    {
                        UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace(
                            $"UpdatePluginsList: Plugin {plugin.Id} - isInstalled={isInstalled}, pluginType={plugin.GetType().Name}, supportsSettings={capabilities.SupportsSettingsPage}, supportsFeaturePage={capabilities.SupportsFeaturePage}, supportsOptimizationCategory={capabilities.SupportsOptimizationCategory}, supportsExecutableEntryPoint={supportsExecutableEntryPoint}");
                    }

                    existingViewModel.SupportsConfiguration = capabilities.SupportsSettingsPage && isInstalled;
                    existingViewModel.SupportsFeaturePage = capabilities.SupportsFeaturePage;
                    existingViewModel.SupportsOptimizationCategory = capabilities.SupportsOptimizationCategory;
                    existingViewModel.SupportsExecutableEntryPoint = supportsExecutableEntryPoint;
                }
                else
                {
                    // Create new ViewModel
                    var pluginViewModel = new PluginViewModel(plugin, isInstalled, updateAvailable, version, isLocal);
                    pluginViewModel.Name = localizedName;
                    pluginViewModel.Description = localizedDescription;
                    pluginViewModel.Tags = localizedTags;
                    pluginViewModel.Location = location;
                    pluginViewModel.NewVersion = newVersion;
                    pluginViewModel.ReleaseDate = releaseDate;
                    pluginViewModel.Changelog = changelog;
                    pluginViewModel.Author = metadata?.Author ?? string.Empty;
                    pluginViewModel.DetailedDescription = detailedDescription;
                    pluginViewModel.UsageGuide = usageGuide;
                    pluginViewModel.SetIconBackgroundFromStore(iconBackground);

                    if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                    {
                        UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace(
                            $"UpdatePluginsList: Plugin {plugin.Id} - isInstalled={isInstalled}, pluginType={plugin.GetType().Name}, supportsSettings={capabilities.SupportsSettingsPage}, supportsFeaturePage={capabilities.SupportsFeaturePage}, supportsOptimizationCategory={capabilities.SupportsOptimizationCategory}, supportsExecutableEntryPoint={supportsExecutableEntryPoint}");
                    }

                    pluginViewModel.SupportsConfiguration = capabilities.SupportsSettingsPage && isInstalled;
                    pluginViewModel.SupportsFeaturePage = capabilities.SupportsFeaturePage;
                    pluginViewModel.SupportsOptimizationCategory = capabilities.SupportsOptimizationCategory;
                    pluginViewModel.SupportsExecutableEntryPoint = supportsExecutableEntryPoint;

                    _pluginViewModels.Add(pluginViewModel);
                }
            }
            catch (Exception ex)
            {
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Failed to update ViewModel for plugin {plugin.Id}: {ex.Message}", ex);
            }
        }

        // Set ListBox data source
        _pluginsListBox.ItemsSource = _pluginViewModels;
        SelectPreferredPlugin(currentPluginIds);

        // Update results count
        if (_resultsCountTextBlock != null)
        {
            _resultsCountTextBlock.Text = string.Format(Resource.PluginExtensionsPage_FoundPluginsCount, uniquePlugins.Count);
            _resultsCountTextBlock.IsVisible = uniquePlugins.Any() ? true : false;
        }

        SyncPluginInstallUi();
    }

    private void SelectPreferredPlugin(HashSet<string> visiblePluginIds)
    {
        if (_pluginsListBox == null)
            return;

        var selectedPluginId = _currentSelectedPluginId;
        if (string.IsNullOrWhiteSpace(selectedPluginId) &&
            _pluginsListBox.SelectedItem is PluginViewModel currentSelection)
        {
            selectedPluginId = currentSelection.PluginId;
        }

        var selectedViewModel = !string.IsNullOrWhiteSpace(selectedPluginId)
            ? _pluginViewModels.FirstOrDefault(vm =>
                visiblePluginIds.Contains(vm.PluginId) &&
                string.Equals(vm.PluginId, selectedPluginId, StringComparison.OrdinalIgnoreCase))
            : null;

        selectedViewModel ??= _pluginViewModels.FirstOrDefault(vm => visiblePluginIds.Contains(vm.PluginId));

        if (selectedViewModel != null)
        {
            if (!ReferenceEquals(_pluginsListBox.SelectedItem, selectedViewModel))
                _pluginsListBox.SelectedItem = selectedViewModel;

            _currentSelectedPluginId = selectedViewModel.PluginId;
            return;
        }

        _pluginsListBox.SelectedItem = null;
        _currentSelectedPluginId = string.Empty;
    }
}
