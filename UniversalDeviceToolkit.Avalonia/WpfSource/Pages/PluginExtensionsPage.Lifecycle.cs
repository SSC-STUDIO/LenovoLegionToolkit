using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;

namespace UniversalDeviceToolkit.WPF.Pages;

public partial class PluginExtensionsPage
{
    private bool _hasStartedInitialFetch;
    private bool _lifecycleSubscriptionsAttached;
    private bool _isPluginInstallCoordinatorSubscribed;

    private async void PluginExtensionsPage_Loaded(object sender, RoutedEventArgs e)
    {
        AttachPageLifecycleSubscriptions();

        // Always paint skeleton + shimmer first, including hot re-entry / page cache hits.
        // Do not call SetPluginResourceCultures here; scanning all assemblies freezes the UI.
        ShowSkeletonImmediate();
        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Loaded);
        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Render);

        // Apply plugin cultures after first paint at low priority.
        _ = Dispatcher.BeginInvoke(
            new Action(() => LocalizationHelper.SetPluginResourceCultures()),
            System.Windows.Threading.DispatcherPriority.Background);

        try
        {
            if (_hasStartedInitialFetch)
            {
                // Re-entry: rebuild under skeleton, honor MinSkeletonVisible, then soft reveal.
                await RebuildPluginListWithoutBlockingAsync();
                SyncPluginInstallUi();
                SetLoadingState(false);

                // Quiet background store refresh (no second skeleton cycle).
                if (!_isLoadingOnlinePlugins)
                    _ = FetchOnlinePluginsAsync(forceRefresh: false, showFullSkeleton: false);
                return;
            }

            _hasStartedInitialFetch = true;

            // Prepare local/installed list under the skeleton (no network yet).
            await RebuildPluginListWithoutBlockingAsync();
            SyncPluginInstallUi();
            var hasLocalContent = _pluginViewModels.Count > 0;

            // Hold skeleton for MinSkeletonVisible, then reveal list. Online fetch can
            // continue without covering the list again when we already have rows.
            if (hasLocalContent)
            {
                SetLoadingState(false); // respects MinSkeletonVisible
                await FetchOnlinePluginsAsync(showFullSkeleton: false);
            }
            else
            {
                // No rows yet: keep skeleton for the whole store fetch (also min-hold).
                await FetchOnlinePluginsAsync(showFullSkeleton: true);
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Trace($"PluginExtensionsPage: initial online plugin fetch failed: {ex.Message}", ex);
            _onlineMetadataLoadFailed = true;
            _onlineMetadataLoadCompleted = true;
            SetLoadingState(false);
            await RebuildPluginListWithoutBlockingAsync();
            UpdateBulkActionButtonsVisibility();
        }
    }

    /// <summary>
    /// Yield to the dispatcher so navigation/skeleton can paint, then rebuild the list.
    /// Keeps heavy-ish work after first frame instead of freezing the click that opened the page.
    /// </summary>
    private async Task RebuildPluginListWithoutBlockingAsync()
    {
        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
        if (!IsLoaded)
            return;
        UpdateAllPluginsUI();
    }

    private void PluginExtensionsPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            AttachPluginInstallCoordinator();
            EnsurePluginExtensionsNavigationState();

            // Skip heavy rebuild while an online fetch is still showing skeleton.
            if (_isLoadingOnlinePlugins)
                return;

            // Background priority so navigation animation stays smooth.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_isLoadingOnlinePlugins || !IsVisible)
                    return;

                // SetPluginResourceCultures is already deferred from Loaded; avoid re-scanning
                // every assembly on every tab show (was a major freeze).
                UpdateAllPluginsUI();
                SyncPluginInstallUi();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    private void PluginExtensionsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        // Invalidate in-flight store fetch so it cannot paint over a new navigation.
        _pageLoadVersion++;
        try
        {
            _pageLoadCts?.Cancel();
            _pageLoadCts?.Dispose();
            _pageLoadCts = null;
        }
        catch
        {
            // Ignore dispose races during navigation teardown.
        }

        SkeletonShimmer.StopSubtree(_loadingIndicator);
        DetachPageLifecycleSubscriptions();
    }

    private void AttachPageLifecycleSubscriptions()
    {
        if (_lifecycleSubscriptionsAttached)
            return;

        IsVisibleChanged += PluginExtensionsPage_IsVisibleChanged;
        _pluginManager.PluginStateChanged += PluginManager_PluginStateChanged;
        AttachPluginInstallCoordinator();
        _lifecycleSubscriptionsAttached = true;
    }

    private void DetachPageLifecycleSubscriptions()
    {
        if (!_lifecycleSubscriptionsAttached)
            return;

        IsVisibleChanged -= PluginExtensionsPage_IsVisibleChanged;
        _pluginManager.PluginStateChanged -= PluginManager_PluginStateChanged;
        DetachPluginInstallCoordinator();
        _lifecycleSubscriptionsAttached = false;
    }

    private void AttachPluginInstallCoordinator()
    {
        if (_isPluginInstallCoordinatorSubscribed)
            return;

        _pluginInstallCoordinator.Changed += PluginInstallCoordinator_Changed;
        _isPluginInstallCoordinatorSubscribed = true;
    }

    private void DetachPluginInstallCoordinator()
    {
        if (!_isPluginInstallCoordinatorSubscribed)
            return;

        _pluginInstallCoordinator.Changed -= PluginInstallCoordinator_Changed;
        _isPluginInstallCoordinatorSubscribed = false;
    }

    private void PluginInstallCoordinator_Changed(object? sender, EventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => PluginInstallCoordinator_Changed(sender, e));
            return;
        }

        SyncPluginInstallUi();
    }

    private void SyncPluginInstallUi()
    {
        if (!_pluginInstallCoordinator.HasPendingWork)
        {
            foreach (var viewModel in _pluginViewModels.Where(vm => vm.IsInstalling))
            {
                viewModel.IsInstalling = false;
                viewModel.InstallProgress = 0;
                viewModel.InstallStatusText = string.Empty;
            }

            return;
        }

        var activePluginId = _pluginInstallCoordinator.PluginId;

        foreach (var viewModel in _pluginViewModels)
        {
            if (_pluginInstallCoordinator.IsActive &&
                !string.IsNullOrWhiteSpace(activePluginId) &&
                string.Equals(viewModel.PluginId, activePluginId, StringComparison.OrdinalIgnoreCase))
            {
                viewModel.IsInstalling = true;
                viewModel.InstallProgress = _pluginInstallCoordinator.Progress;
                viewModel.InstallStatusText = _pluginInstallCoordinator.StatusText;
                continue;
            }

            if (_pluginInstallCoordinator.IsQueued(viewModel.PluginId))
            {
                viewModel.IsInstalling = true;
                viewModel.InstallProgress = 0;
                viewModel.InstallStatusText = Resource.PluginExtensionsPage_InstallQueued;
                continue;
            }

            if (viewModel.IsInstalling)
            {
                viewModel.IsInstalling = false;
                viewModel.InstallProgress = 0;
                viewModel.InstallStatusText = string.Empty;
            }
        }
    }
}
