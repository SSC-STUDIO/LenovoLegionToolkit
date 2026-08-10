using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Windows;
using PluginManifest = UniversalDeviceToolkit.Lib.Plugins.PluginManifest;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class PluginExtensionsPage

{
    private async void BulkUpdateButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!_availableUpdates.Any()) return;

        try
        {
            _bulkUpdateButton.IsEnabled = false;
            _bulkUpdateButton.Content = Resource.PluginExtensionsPage_UpdatingAll;

            SnackbarHelper.Show(Resource.PluginExtensionsPage_UpdatingPlugin, string.Format(Resource.PluginExtensionsPage_UpdatingPluginMessage, _availableUpdates.Count), SnackbarType.Info);

            // Use a copy to avoid modification during iteration if needed,
            // but here we just need the IDs and manifests
            var updatesToProcess = _availableUpdates.ToList();

            foreach (var update in updatesToProcess)
            {
                try
                {
                    await InstallOnlinePluginAsync(update, navigateToOptimizationCategoryOnSuccess: false);
                }
                catch (Exception ex)
                {
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Error during bulk update for {update.Id}: {ex.Message}", ex);
                }
            }

            SnackbarHelper.Show(Resource.PluginExtensionsPage_BulkUpdateComplete, Resource.PluginExtensionsPage_BulkUpdateCompleteMessage, SnackbarType.Success);
        }
        catch (Exception ex)
        {
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Error in bulk update: {ex.Message}", ex);
            SnackbarHelper.Show(Resource.PluginExtensionsPage_UpdateFailed, string.Format(Resource.PluginExtensionsPage_UpdateFailedMessage, ex.Message), SnackbarType.Error);
        }
        finally
        {
            _bulkUpdateButton.IsEnabled = true;
            _bulkUpdateButton.Content = Resource.PluginExtensionsPage_UpdateAll;

            try
            {
                await FetchOnlinePluginsAsync();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error refreshing plugins after bulk update: {ex.Message}", ex);
            }
        }
    }

    private async void BulkInstallButton_Click(object? sender, RoutedEventArgs e)
    {
        var installCandidates = _onlinePlugins
            .Where(plugin => !IsPluginInstalledForUi(plugin.Id))
            .ToList();

        if (!installCandidates.Any())
            return;

        var installAllText = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_InstallAll", "Install All", Resource.Culture);
        var installingAllText = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_InstallingAll", "Installing All...", Resource.Culture);
        var installingAllMessage = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_InstallingAllMessage", "Installing {0} plugin(s)...", Resource.Culture);
        var bulkInstallComplete = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_BulkInstallComplete", "Bulk Install Complete", Resource.Culture);
        var bulkInstallCompleteMessage = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_BulkInstallCompleteMessage", "Installed {0} plugin(s).", Resource.Culture);
        var bulkInstallFailed = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_BulkInstallFailed", "Bulk Install Failed", Resource.Culture);
        var bulkInstallFailedMessage = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_BulkInstallFailedMessage", "Failed to install plugins: {0}", Resource.Culture);

        try
        {
            if (_bulkInstallButton != null)
            {
                _bulkInstallButton.IsEnabled = false;
                _bulkInstallButton.Content = installingAllText;
            }

            if (_bulkUpdateButton != null)
                _bulkUpdateButton.IsEnabled = false;

            SnackbarHelper.Show(installingAllText, string.Format(installingAllMessage, installCandidates.Count), SnackbarType.Info);

            var installedCount = 0;
            foreach (var candidate in installCandidates)
            {
                try
                {
                    await InstallOnlinePluginAsync(candidate, navigateToOptimizationCategoryOnSuccess: false);

                    if (IsPluginInstalledForUi(candidate.Id))
                        installedCount++;
                }
                catch (Exception ex)
                {
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Error during bulk install for {candidate.Id}: {ex.Message}", ex);
                }
            }

            SnackbarHelper.Show(bulkInstallComplete, string.Format(bulkInstallCompleteMessage, installedCount), SnackbarType.Success);
        }
        catch (Exception ex)
        {
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Error in bulk install: {ex.Message}", ex);
            SnackbarHelper.Show(bulkInstallFailed, string.Format(bulkInstallFailedMessage, ex.Message), SnackbarType.Error);
        }
        finally
        {
            if (_bulkInstallButton != null)
            {
                _bulkInstallButton.IsEnabled = true;
                _bulkInstallButton.Content = installAllText;
            }

            if (_bulkUpdateButton != null)
                _bulkUpdateButton.IsEnabled = true;

            try
            {
                await FetchOnlinePluginsAsync();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error refreshing plugins after bulk install: {ex.Message}", ex);
            }
        }
    }
    private async void PluginInstallButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not UniversalDeviceToolkit.Avalonia.Controls.Button button || button.Tag is not string pluginId)
            return;

        try
        {
            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"PluginInstallButton_Click called for {pluginId}");
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"  - IsInstalled before install: {_pluginManager.IsInstalled(pluginId)}");
            }

            // Check if this is an online plugin installation
            var onlinePlugin = _onlinePlugins.FirstOrDefault(p => p.Id == pluginId);
            if (onlinePlugin != null)
            {
                if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Installing online plugin: {pluginId}");
                }
                await InstallOnlinePluginAsync(onlinePlugin);
                return;
            }

            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Installing local plugin: {pluginId}");
            }

            // If plugin is already installed, uninstall it first to release file locks
            if (_pluginManager.IsInstalled(pluginId))
            {

                if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Plugin {pluginId} is already installed, uninstalling first to release file locks");
                }
                // Stop plugin before uninstallation to release resources
                _pluginManager.StopPlugin(pluginId);
                _pluginManager.UninstallPlugin(pluginId);

                // Wait a moment for the uninstall to complete
                await Task.Delay(1000);
            }

            _pluginManager.InstallPlugin(pluginId);

            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"  - IsInstalled after install: {_pluginManager.IsInstalled(pluginId)}");
            }

            await RefreshInstalledPluginUiAfterInstallAsync(pluginId, forceRefreshRuntime: true);
            await ShowInstalledPluginFeedbackAsync(pluginId);
        }
        catch (Exception ex)
        {
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Error installing plugin: {ex.Message}", ex);

            if (TopLevel.GetTopLevel(this) is MainWindow mainWindow)
            {
                SnackbarHelper.Show(Resource.PluginExtensionsPage_InstallFailed, string.Format(Resource.PluginExtensionsPage_InstallFailedMessage, ex.Message), SnackbarType.Error);
            }
        }
    }

    private async Task InstallOnlinePluginAsync(PluginManifest manifest, bool navigateToOptimizationCategoryOnSuccess = true)
    {
        if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
        {
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"InstallOnlinePluginAsync started for {manifest.Id}");
        }

        try
        {
            var versionChecker = new VersionChecker();
            if (!versionChecker.IsCompatible(manifest.MinimumHostVersion))
            {
                SnackbarHelper.Show(
                    Resource.PluginExtensionsPage_InstallFailed,
                    string.Format(
                        Resource.Culture ?? CultureInfo.CurrentUICulture,
                        Resource.PluginExtensionsPage_MinimumVersion,
                        manifest.MinimumHostVersion),
                    SnackbarType.Warning);
                return;
            }

            var installTask = _pluginInstallCoordinator.InstallAsync(manifest);
            SyncPluginInstallUi();

            var success = await installTask;

            if (success)
            {
                _recentInstalledVersions[manifest.Id] = manifest.Version;
                RemoveAvailableUpdate(manifest.Id);
                ReconcileAvailableUpdatesWithInstalledVersions();
                await RefreshInstalledPluginUiAfterInstallAsync(manifest.Id, forceRefreshRuntime: true);

                if (navigateToOptimizationCategoryOnSuccess)
                    await ShowInstalledPluginFeedbackAsync(manifest.Id, manifest);
                else
                    SnackbarHelper.Show(Resource.PluginExtensionsPage_InstallSuccess, string.Format(Resource.PluginExtensionsPage_InstallSuccessMessage, manifest.Name), SnackbarType.Success);
            }
            else
            {
                SnackbarHelper.Show(
                    Resource.PluginExtensionsPage_InstallFailed,
                    T("PluginExtensionsPage_InstallFailedWithoutDetailsMessage", "Plugin could not be installed. Please try again."),
                    SnackbarType.Error);

                UpdateSpecificPluginUI(manifest.Id);
            }
        }
        catch (Exception ex)
        {
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Error installing online plugin {manifest.Id}: {ex.Message}", ex);

            SnackbarHelper.Show(
                Resource.PluginExtensionsPage_InstallFailed,
                string.Format(
                    Resource.Culture ?? CultureInfo.CurrentUICulture,
                    Resource.PluginExtensionsPage_InstallFailedMessage,
                    ex.Message),
                SnackbarType.Error);
        }
        finally
        {
            UpdateSpecificPluginUI(manifest.Id);
        }
    }

    private async Task ShowInstalledPluginFeedbackAsync(string pluginId, PluginManifest? fallbackManifest = null)
    {
        var plugin = await GetRegisteredPluginForUiAsync(pluginId, forceRefresh: true);
        var manifestMetadata = ResolvePluginManifestMetadata(pluginId) ?? fallbackManifest;
        var runtimeCapabilities = plugin is null ? default : ResolveRuntimePluginCapabilities(plugin);
        var manifestCapabilities = PluginUiCapabilityResolver
            .ResolveFromManifest(manifestMetadata)
            .Merge(PluginUiCapabilityResolver.ResolveFromInstalledManifest(pluginId));
        var capabilities = ResolvePluginCapabilities(plugin, true, pluginId, manifestMetadata);
        var hasExecutable = TryResolvePluginExecutable(pluginId, out _, out _);
        var feedback = ResolveInstalledPluginFeedback(runtimeCapabilities, manifestCapabilities, hasExecutable, plugin is null);

        if (feedback == InstalledPluginFeedback.EntryAvailable &&
            ShouldNavigateToOptimizationAfterInstall(capabilities, hasExecutable))
        {
            if (NavigateToPluginOptimizationCategory(pluginId))
            {
                SnackbarHelper.Show(
                    Resource.PluginExtensionsPage_InstallSuccess,
                    string.Format(
                        Resource.Culture ?? CultureInfo.CurrentUICulture,
                        T("PluginExtensionsPage_InstallSuccessOptimizationMessage", "Plugin {0} was installed and opened in System Optimization."),
                        GetInstalledPluginFeedbackName(plugin, pluginId, manifestMetadata)),
                    SnackbarType.Success);
                return;
            }
        }

        var pluginName = GetInstalledPluginFeedbackName(plugin, pluginId, manifestMetadata);

        if (feedback == InstalledPluginFeedback.EntryAvailable)
        {
            SnackbarHelper.Show(
                Resource.PluginExtensionsPage_InstallSuccess,
                string.Format(
                    Resource.Culture ?? CultureInfo.CurrentUICulture,
                    T("PluginExtensionsPage_InstallSuccessWithEntryMessage", "Plugin {0} was installed. Use Open to launch its available entry point."),
                    pluginName),
                SnackbarType.Success);
            return;
        }

        SnackbarHelper.Show(
            T("PluginExtensionsPage_InstalledButNoEntryTitle", "Installed, but no entry point"),
            string.Format(
                Resource.Culture ?? CultureInfo.CurrentUICulture,
                feedback == InstalledPluginFeedback.RuntimeNotLoaded
                    ? T("PluginExtensionsPage_InstalledButRuntimeUnavailableMessage", "Plugin {0} was installed, but its runtime UI could not be loaded. Restart the app or reinstall the plugin.")
                    : T("PluginExtensionsPage_InstalledButNoEntryMessage", "Plugin {0} was installed, but it does not expose a user-facing entry point. It may only provide background services or manifest data."),
                pluginName),
            feedback == InstalledPluginFeedback.RuntimeNotLoaded ? SnackbarType.Warning : SnackbarType.Info);
    }

    internal static bool ShouldNavigateToOptimizationAfterInstall(PluginUiCapabilities capabilities, bool hasExecutable) =>
        capabilities.SupportsOptimizationCategory &&
        !capabilities.SupportsFeaturePage &&
        !capabilities.SupportsSettingsPage &&
        !hasExecutable;

    internal enum InstalledPluginFeedback
    {
        EntryAvailable,
        RuntimeNotLoaded,
        NoUserFacingEntry
    }

    internal static InstalledPluginFeedback ResolveInstalledPluginFeedback(
        PluginUiCapabilities runtimeCapabilities,
        PluginUiCapabilities manifestCapabilities,
        bool hasExecutable,
        bool runtimeMissing)
    {
        if (runtimeCapabilities.HasAny || hasExecutable)
            return InstalledPluginFeedback.EntryAvailable;

        if (manifestCapabilities.SupportsOptimizationCategory &&
            !manifestCapabilities.SupportsFeaturePage &&
            !manifestCapabilities.SupportsSettingsPage)
        {
            return InstalledPluginFeedback.EntryAvailable;
        }

        if (runtimeMissing && manifestCapabilities.HasAny)
            return InstalledPluginFeedback.RuntimeNotLoaded;

        if (!runtimeMissing && manifestCapabilities.HasAny)
            return InstalledPluginFeedback.EntryAvailable;

        return runtimeMissing
            ? InstalledPluginFeedback.RuntimeNotLoaded
            : InstalledPluginFeedback.NoUserFacingEntry;
    }

    private string GetInstalledPluginFeedbackName(IPlugin? plugin, string pluginId, PluginManifest? manifest)
    {
        if (plugin is not null)
            return GetPluginLocalizedName(plugin, manifest);

        if (manifest is not null)
            return GetPluginLocalizedName(new PluginManifestAdapter(manifest), manifest);

        return pluginId;
    }

    private async Task RefreshInstalledPluginUiAfterInstallAsync(string pluginId, bool forceRefreshRuntime)
    {
        _pluginIdsReloadedForUi.Remove(pluginId);
        PluginUiCapabilityResolver.InvalidateCache(pluginId);
        await _pluginManager.ScanAndLoadPluginsAsync(forceRefreshRuntime);
        LocalizationHelper.SetPluginResourceCultures();
        UpdateAllPluginsUI();

        if (TopLevel.GetTopLevel(this) is MainWindow mainWindow)
            mainWindow.UpdateInstalledPluginsNavigationItems();
    }

    private async void PluginUninstallButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not UniversalDeviceToolkit.Avalonia.Controls.Button button || button.Tag is not string pluginId)
            return;

        if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"PluginUninstallButton_Click called for {pluginId}");

        try
        {
            // For local plugins, we should ensure any running processes are stopped
            if (_pluginManager.IsInstalled(pluginId))
            {
                if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Stopping plugin {pluginId} before uninstall");

                // Stop the plugin first
                _pluginManager.StopPlugin(pluginId);
            }

            var result = await Task.Run(() => _pluginManager.UninstallPlugin(pluginId));

            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"UninstallPlugin returned: {result}");

            if (!result)
            {
                SnackbarHelper.Show(
                    Resource.PluginExtensionsPage_UninstallFailed,
                    T("PluginExtensionsPage_UninstallDependencyMessage", "Plugin could not be uninstalled. It might be a dependency for another plugin."),
                    SnackbarType.Error);
                return;
            }

            // Immediately update specific plugin's UI state
            _pluginIdsReloadedForUi.Remove(pluginId);
            PluginUiCapabilityResolver.InvalidateCache(pluginId);
            UpdateSpecificPluginUI(pluginId);

            SnackbarHelper.Show(Resource.PluginExtensionsPage_UninstallSuccess, Resource.PluginExtensionsPage_UninstallSuccessMessage, SnackbarType.Success);
        }
        catch (Exception ex)
        {
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Error uninstalling plugin: {ex.Message}", ex);

            SnackbarHelper.Show(Resource.PluginExtensionsPage_UninstallFailed, string.Format(Resource.PluginExtensionsPage_UninstallFailedMessage, ex.Message), SnackbarType.Error);
        }
    }


}
