using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Pages;

public partial class PluginExtensionsPage
{
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        _isRefreshing = true;
        var refreshButton = this.FindName("_refreshButton") as Wpf.Ui.Controls.Button;
        if (refreshButton != null)
        {
            refreshButton.IsEnabled = false;
            refreshButton.Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowSync24 };
        }

        try
        {
            // Manual refresh: keep list when we already have cards (classic smooth path).
            // Full skeleton only when the page is empty so first-load shimmer still appears.
            var showFullSkeleton = _pluginViewModels.Count == 0;
            await FetchOnlinePluginsAsync(forceRefresh: true, showFullSkeleton: showFullSkeleton);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(RefreshButton_Click)}: {ex.Message}", ex);
        }
        finally
        {
            _isRefreshing = false;
            if (refreshButton != null)
            {
                refreshButton.IsEnabled = true;
                refreshButton.Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowClockwise24 };
            }
        }
    }

    private async Task FetchOnlinePluginsAsync(bool forceRefresh = false, bool showFullSkeleton = true)
    {
        if (_isLoadingOnlinePlugins)
            return;

        var version = ++_pageLoadVersion;
        _isLoadingOnlinePlugins = true;
        _onlineMetadataLoadCompleted = false;
        _onlineMetadataLoadFailed = false;
        _pageUiState = showFullSkeleton ? PluginPageUiState.InitialLoading : PluginPageUiState.Refreshing;

        _pageLoadCts?.Cancel();
        _pageLoadCts?.Dispose();
        _pageLoadCts = new CancellationTokenSource();
        var token = _pageLoadCts.Token;

        try
        {
            // Only cover the list with skeleton when there is nothing useful to show yet.
            if (showFullSkeleton)
                SetLoadingState(true);

            _availableUpdates.Clear();

            // 15s timeout - never leave the page blank waiting on the store forever.
            var fetchTask = _pluginRepositoryService.FetchAvailablePluginsAsync(forceRefresh);
            var completed = await Task.WhenAny(fetchTask, Task.Delay(OnlineFetchTimeout, token));
            if (version != _pageLoadVersion || token.IsCancellationRequested)
            {
                // Observe abandoned fetch so faults are not unobserved.
                _ = ObserveAbandonedFetchAsync(fetchTask);
                return;
            }

            if (completed != fetchTask)
            {
                _ = ObserveAbandonedFetchAsync(fetchTask);
                _onlineMetadataLoadFailed = true;
                _pageUiState = PluginPageUiState.Offline;
                // Keep any previously loaded store list; only clear updates we cannot verify.
                _availableUpdates.Clear();
                SnackbarHelper.Show(
                    T("PluginExtensionsPage_FetchFailed", "Failed to fetch plugins"),
                    T("PluginExtensionsPage_FetchTimeoutMessage", "Store request timed out. Installed plugins are still available."),
                    SnackbarType.Warning);
            }
            else
            {
                _onlinePlugins = await fetchTask;
                if (version != _pageLoadVersion)
                    return;

                _onlineMetadataLoadFailed = false;
                if (Log.Instance.IsTraceEnabled)
                {
                    Log.Instance.Trace($"PluginExtensionsPage: Fetched {_onlinePlugins.Count} online plugins");
                }

                try
                {
                    var installedManifests = BuildInstalledPluginManifestsForUpdateCheck();
                    var updates = await _pluginRepositoryService.CheckForUpdatesAsync(installedManifests);
                    if (version != _pageLoadVersion)
                        return;
                    _availableUpdates = updates;
                    ReconcileAvailableUpdatesWithInstalledVersions();
                }
                catch (Exception ex)
                {
                    Log.Instance.Trace(
                        $"PluginExtensionsPage: update check failed after online plugins were loaded: {ex.Message}",
                        ex);
                    _availableUpdates.Clear();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Page left - ignore stale result.
            return;
        }
        catch (Exception ex)
        {
            if (version != _pageLoadVersion)
                return;

            _onlineMetadataLoadFailed = true;
            _pageUiState = PluginPageUiState.Failed;
            // Do not wipe _onlinePlugins on hard failure - keep last good store snapshot.
            _availableUpdates.Clear();
            Log.Instance.Trace($"Error fetching online plugins: {ex.Message}", ex);

            SnackbarHelper.Show(
                T("PluginExtensionsPage_FetchFailed", "Failed to fetch plugins"),
                string.Format(
                    Resource.Culture ?? CultureInfo.CurrentUICulture,
                    T("PluginExtensionsPage_FetchFailedMessage", "Unable to get plugin list from store: {0}"),
                    ex.Message),
                SnackbarType.Error);
        }
        finally
        {
            if (version == _pageLoadVersion)
            {
                _onlineMetadataLoadCompleted = true;
                _isLoadingOnlinePlugins = false;
                SetLoadingState(false);
                UpdateAllPluginsUI();
                UpdateBulkActionButtonsVisibility();
                // Prefer Offline/Failed flags over "Ready just because local rows exist".
                if (_onlineMetadataLoadFailed)
                {
                    _pageUiState = _pageUiState is PluginPageUiState.Offline
                        ? PluginPageUiState.Offline
                        : PluginPageUiState.Failed;
                }
                else
                {
                    _pageUiState = _pluginViewModels.Count > 0
                        ? PluginPageUiState.Ready
                        : PluginPageUiState.Empty;
                }

                UpdateStoreOfflineBanner();
            }
            else
            {
                _isLoadingOnlinePlugins = false;
            }
        }
    }

    private static async Task ObserveAbandonedFetchAsync(Task fetchTask)
    {
        try
        {
            await fetchTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Abandoned plugin store fetch faulted.", ex);
        }
    }

    private void UpdateStoreOfflineBanner()
    {
        if (_storeOfflineBanner is null)
            return;

        var show = _onlineMetadataLoadFailed || _pageUiState is PluginPageUiState.Offline or PluginPageUiState.Failed;
        _storeOfflineBanner.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (!show)
            return;

        if (_storeOfflineTitleText is not null)
        {
            _storeOfflineTitleText.Text = _pageUiState == PluginPageUiState.Offline
                ? LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_StoreTimeoutTitle", "Plugin store timed out", Resource.Culture)
                : LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_StoreUnavailableTitle", "Plugin store unavailable", Resource.Culture);
        }

        if (_storeOfflineMessageText is not null)
        {
            _storeOfflineMessageText.Text = LocalizationHelper.GetStringOrEnglish(
                Resource.ResourceManager,
                "PluginExtensionsPage_StoreUnavailableMessage",
                "Installed plugins remain available. Retry when the network is back.",
                Resource.Culture);
        }

        if (_storeRetryButton is not null)
        {
            _storeRetryButton.Content = LocalizationHelper.GetStringOrEnglish(
                Resource.ResourceManager,
                "PluginExtensionsPage_StoreRetry",
                "Retry",
                Resource.Culture);
            _storeRetryButton.IsEnabled = !_isLoadingOnlinePlugins && !_isRefreshing;
        }
    }

    private async void StoreRetryButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isLoadingOnlinePlugins || _isRefreshing)
            return;

        if (_storeRetryButton is not null)
            _storeRetryButton.IsEnabled = false;

        try
        {
            // Retry keeps installed list visible; skeleton only if nothing to show.
            var showFullSkeleton = _pluginViewModels.Count == 0;
            await FetchOnlinePluginsAsync(forceRefresh: true, showFullSkeleton);
        }
        finally
        {
            UpdateStoreOfflineBanner();
        }
    }
}
