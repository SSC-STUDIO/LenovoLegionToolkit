using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Collections.ObjectModel;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Controls.Loading;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Windows;
using PluginConstants = UniversalDeviceToolkit.Lib.Plugins.PluginConstants;
using Wpf.Ui.Controls;
using NavigationItem = UniversalDeviceToolkit.WPF.Controls.Custom.NavigationItem;
using NavigationStore = UniversalDeviceToolkit.WPF.Controls.Custom.NavigationStore;
using PluginManifest = UniversalDeviceToolkit.Lib.Plugins.PluginManifest;

namespace UniversalDeviceToolkit.WPF.Pages
{
[LoadingChromeOwner(LoadingChromeOwnership.Page, delayMilliseconds: 0, minimumVisibleMilliseconds: 520)]
public partial class PluginExtensionsPage : ILoadingChromeOwner
{
    private readonly IPluginManager _pluginManager = IoCContainer.Resolve<IPluginManager>();
    private readonly PluginRepositoryService _pluginRepositoryService = IoCContainer.Resolve<PluginRepositoryService>();
    private readonly PluginInstallCoordinator _pluginInstallCoordinator = IoCContainer.Resolve<PluginInstallCoordinator>();

private string _currentSearchText = string.Empty;
    private string _currentFilter = "All";
    private List<IPlugin> _allPlugins = new();
    private List<PluginManifest> _onlinePlugins = new();
    private List<PluginManifest> _availableUpdates = new();
    private ObservableCollection<PluginViewModel> _pluginViewModels = new();
    private string _currentSelectedPluginId = string.Empty;
    private bool _isRefreshing = false;
    private bool _isLoadingOnlinePlugins = false;
    private bool _hasStartedInitialFetch = false;
    private bool _onlineMetadataLoadCompleted = false;
    private bool _onlineMetadataLoadFailed = false;
    private readonly Dictionary<string, string> _recentInstalledVersions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _installedStateSnapshot = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _pluginIdsReloadedForUi = new(StringComparer.OrdinalIgnoreCase);
    private bool _isPluginInstallCoordinatorSubscribed;

    /// <summary>
    /// Minimum skeleton hold so shimmer is actually perceived (including re-entry), without
    /// a multi-second artificial freeze (was 1500ms). Long enough to outlast nav handoff.
    /// </summary>
    private static readonly TimeSpan MinSkeletonVisible = TimeSpan.FromMilliseconds(520);
    private static readonly TimeSpan OnlineFetchTimeout = TimeSpan.FromSeconds(15);
    private CancellationTokenSource? _pageLoadCts;
    private int _pageLoadVersion;
    private PluginPageUiState _pageUiState = PluginPageUiState.InitialLoading;

    private enum PluginPageUiState
    {
        InitialLoading,
        Refreshing,
        Ready,
        Empty,
        Offline,
        Failed
    }
    private DateTime _skeletonShownAtUtc = DateTime.MinValue;
    private bool _skeletonSubtreeLayoutPrimed;
    private int _loadingStateVersion;
    private bool _lifecycleSubscriptionsAttached;
    private readonly DebounceDispatcher _searchDebouncer = new();

    public LoadingChromeOwnership LoadingChromeOwnership => LoadingChromeOwnership.Page;

    public PluginExtensionsPage()
    {
        InitializeComponent();
        Loaded += PluginExtensionsPage_Loaded;
        Unloaded += PluginExtensionsPage_Unloaded;
        AttachPageLifecycleSubscriptions();

        // Initialize ListBox data binding
        if (_pluginsListBox != null)
        {
            _pluginsListBox.ItemsSource = _pluginViewModels;
        }

        // Instant skeleton (no fade-from-0) so the first frame is never blank/white.
        ShowSkeletonImmediate();

        // Set page title and text (using dynamic resources to avoid auto-generated resource issues)
        Title = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_Title", "Plugin Extensions", Resource.Culture);

        var titleTextBlock = this.FindName("_titleTextBlock") as System.Windows.Controls.TextBlock;
        if (titleTextBlock != null)
        {
            titleTextBlock.Text = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_Title", "Plugin Extensions", Resource.Culture);
        }

        var descriptionTextBlock = this.FindName("_descriptionTextBlock") as System.Windows.Controls.TextBlock;
        if (descriptionTextBlock != null)
        {
            descriptionTextBlock.Text = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_Description", "Install and manage plugins to extend functionality", Resource.Culture);
        }

        if (_bulkInstallButton != null)
        {
            _bulkInstallButton.Content = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_InstallAll", "Install All", Resource.Culture);
            _bulkInstallButton.ToolTip = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_InstallAllTooltip", "Install all available plugins", Resource.Culture);
        }

        if (_bulkImportButton != null)
        {
            _bulkImportButton.Content = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_ImportFromFiles", "Import from Files", Resource.Culture);
            _bulkImportButton.ToolTip = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_BulkImportTooltip", "Import plugins from local ZIP files", Resource.Culture);
            _bulkImportButton.Visibility = Visibility.Visible;
        }

        UpdateSummaryMetrics();
    }

    private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.TextBox textBox)
        {
            _currentSearchText = textBox.Text ?? string.Empty;
            _searchDebouncer.Debounce(300, ApplyFilters);
        }
    }

    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item && item.Tag is string filter)
        {
            _currentFilter = filter;
            ApplyFilters();
        }
    }

    private void PluginsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_pluginsListBox?.SelectedItem is PluginViewModel viewModel)
            _currentSelectedPluginId = viewModel.PluginId;
    }

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

    private void SetLoadingState(bool isLoading)
    {
        if (isLoading)
        {
            ShowSkeletonImmediate();
            return;
        }

        // Honor minimum skeleton visibility so first-open shimmer is actually seen.
        var version = ++_loadingStateVersion;
        var elapsed = _skeletonShownAtUtc == DateTime.MinValue
            ? MinSkeletonVisible
            : DateTime.UtcNow - _skeletonShownAtUtc;
        var remaining = MinSkeletonVisible - elapsed;
        if (remaining > TimeSpan.Zero && IsLoaded)
        {
            _ = HideLoadingStateAfterAsync(remaining, version);
            return;
        }

        ApplyLoadingStateHidden();
    }

    /// <summary>
    /// First-step chrome: skeleton fully opaque and list collapsed. Never fade-from-0
    /// (that left a blank white region until the fade finished, or forever if interrupted).
    /// Always restarts the hold clock so re-entry cannot skip shimmer because of a stale timestamp.
    /// </summary>
    private void ShowSkeletonImmediate()
    {
        _loadingStateVersion++;

        // Nav crossfade may have left this Page at Opacity 0 after a prior leave — force visible.
        BeginAnimation(UIElement.OpacityProperty, null);
        Opacity = 1;

        if (_noPluginsMessage != null)
            _noPluginsMessage.Visibility = Visibility.Collapsed;
        if (_noResultsStackPanel != null)
            _noResultsStackPanel.Visibility = Visibility.Collapsed;

        // List must not cover skeleton (even empty ListBox can paint a blank surface).
        if (_pluginListPanel is FrameworkElement listPanel)
        {
            listPanel.BeginAnimation(UIElement.OpacityProperty, null);
            listPanel.Visibility = Visibility.Collapsed;
            listPanel.Opacity = 1;
            listPanel.IsHitTestVisible = false;
        }

        var skeletonAlreadyLive = _loadingIndicator is FrameworkElement existing
            && existing.Visibility == Visibility.Visible
            && existing.Opacity >= 0.95;

        // Only reset min-hold clock when skeleton is newly shown (classic soft re-entry).
        if (!skeletonAlreadyLive || _skeletonShownAtUtc == DateTime.MinValue)
            _skeletonShownAtUtc = DateTime.UtcNow;

        if (_loadingIndicator is FrameworkElement skeleton)
        {
            skeleton.BeginAnimation(UIElement.OpacityProperty, null);
            skeleton.Visibility = Visibility.Visible;
            skeleton.Opacity = 1;
            skeleton.IsHitTestVisible = true;
            Panel.SetZIndex(skeleton, 2);
            // One-time layout prime: XAML defaults Visible so skeletonAlreadyLive is true on
            // first paint — still need a single measure pass before walking shimmer borders.
            if (!_skeletonSubtreeLayoutPrimed)
            {
                skeleton.UpdateLayout();
                _skeletonSubtreeLayoutPrimed = true;
            }
        }

        // Soft restart: keep phase of already-running sweeps (4.x-style smoothness).
        SkeletonShimmer.RestartSubtree(_loadingIndicator, force: !skeletonAlreadyLive);
    }

    private async Task HideLoadingStateAfterAsync(TimeSpan delay, int version)
    {
        try
        {
            await Task.Delay(delay);
        }
        catch
        {
            return;
        }

        if (version != _loadingStateVersion || !IsLoaded)
            return;

        ApplyLoadingStateHidden();
    }

    private void ApplyLoadingStateHidden()
    {
        _skeletonShownAtUtc = DateTime.MinValue;
        CrossfadeToContent();
    }

    /// <summary>
    /// Soft handoff skeleton → real list only. Skeleton show path always snaps in
    /// via <see cref="ShowSkeletonImmediate"/> (never opacity 0).
    /// </summary>
    private void CrossfadeToContent()
    {
        var duration = TryFindResource("AnimationDurationSkeletonCrossfade") as Duration?
                       ?? new Duration(TimeSpan.FromMilliseconds(220));

        if (_loadingIndicator is FrameworkElement skeleton && skeleton.Visibility == Visibility.Visible)
        {
            SkeletonShimmer.StopSubtree(_loadingIndicator);
            skeleton.IsHitTestVisible = false;
            skeleton.BeginAnimation(UIElement.OpacityProperty, null);
            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = duration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            fadeOut.Completed += (_, _) =>
            {
                // Another ShowSkeletonImmediate may have re-shown it mid-fade.
                if (skeleton.Opacity > 0.05 && skeleton.Visibility == Visibility.Visible)
                    return;
                skeleton.Visibility = Visibility.Collapsed;
                skeleton.BeginAnimation(UIElement.OpacityProperty, null);
                skeleton.Opacity = 1;
            };
            skeleton.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
        else if (_loadingIndicator is not null)
        {
            _loadingIndicator.Visibility = Visibility.Collapsed;
        }

        if (_pluginListPanel is FrameworkElement listPanel)
        {
            listPanel.BeginAnimation(UIElement.OpacityProperty, null);
            listPanel.Visibility = Visibility.Visible;
            listPanel.IsHitTestVisible = true;
            Panel.SetZIndex(listPanel, 1);
            // Content can fade in; skeleton already covered first paint.
            if (listPanel.Opacity < 0.95)
            {
                listPanel.Opacity = 0;
                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = duration,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                listPanel.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            }
            else
            {
                listPanel.Opacity = 1;
            }
        }
    }

    private void UpdateBulkActionButtonsVisibility()
    {
        ReconcileAvailableUpdatesWithInstalledVersions();

        if (_bulkUpdateButton != null)
            _bulkUpdateButton.Visibility = _availableUpdates.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        if (_bulkInstallButton != null)
        {
            var hasInstallCandidates = _onlinePlugins.Any(plugin => !IsPluginInstalledForUi(plugin.Id));
            _bulkInstallButton.Visibility = hasInstallCandidates ? Visibility.Visible : Visibility.Collapsed;
            _bulkInstallButton.ToolTip = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_InstallAllTooltip", "Install all available plugins", Resource.Culture);
        }

        if (_bulkImportButton != null)
            _bulkImportButton.Visibility = Visibility.Visible;

        UpdateSummaryMetrics();
    }

    private void UpdateSummaryMetrics()
    {
        if (_summaryTotalTextBlock == null ||
            _summaryInstalledTextBlock == null ||
            _summaryUpdatesTextBlock == null ||
            _summaryStorePulseValueTextBlock == null ||
            _summaryHintTextBlock == null)
        {
            return;
        }

        var totalPlugins = _allPlugins.Count;
        var installedPlugins = _allPlugins.Count(plugin => IsPluginInstalledForUi(plugin.Id));
        var updatesReady = _availableUpdates.Count;
        var discoverablePlugins = Math.Max(0, totalPlugins - installedPlugins);
        var isWaitingForMetadata = totalPlugins == 0 && !_onlineMetadataLoadCompleted;

        _summaryTotalTextBlock.Text = totalPlugins.ToString(CultureInfo.InvariantCulture);
        _summaryInstalledTextBlock.Text = installedPlugins.ToString(CultureInfo.InvariantCulture);
        _summaryUpdatesTextBlock.Text = updatesReady.ToString(CultureInfo.InvariantCulture);
        _summaryStorePulseValueTextBlock.Text = updatesReady > 0
            ? updatesReady.ToString(CultureInfo.InvariantCulture)
            : discoverablePlugins > 0
                ? discoverablePlugins.ToString(CultureInfo.InvariantCulture)
                : isWaitingForMetadata
                    ? "..."
                    : "0";

        _summaryHintTextBlock.Text = updatesReady > 0
            ? LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_SummaryUpdatesAvailableLabel", "Updates available", Resource.Culture)
            : discoverablePlugins > 0
                ? LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_SummaryDiscoverableLabel", "Available to install", Resource.Culture)
                : isWaitingForMetadata
                    ? LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_SummaryWaitingMetadataShort", "Loading metadata", Resource.Culture)
                    : _onlineMetadataLoadFailed && totalPlugins == 0
                        ? LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_FetchFailed", "Failed to fetch plugins", Resource.Culture)
                        : totalPlugins == 0
                            ? Resource.PluginExtensionsPage_NoPluginsAvailable
                            : LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_SummaryUpToDateShort", "Up to date", Resource.Culture);
    }

    private static string FormatReleaseDate(string releaseDateRaw)
    {
        if (string.IsNullOrWhiteSpace(releaseDateRaw))
            return string.Empty;

        if (!DateTimeOffset.TryParse(releaseDateRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var releaseDate))
            return releaseDateRaw;

        return releaseDate.ToLocalTime().ToString(LocalizationHelper.ShortDateFormat);
    }

    private static string T(string key, string fallback)
    {
        return LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);
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

            // 15s timeout — never leave the page blank waiting on the store forever.
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
                if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace(
                        $"PluginExtensionsPage: Fetched {_onlinePlugins.Count} online plugins");
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
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace(
                        $"PluginExtensionsPage: update check failed after online plugins were loaded: {ex.Message}",
                        ex);
                    _availableUpdates.Clear();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Page left — ignore stale result.
            return;
        }
        catch (Exception ex)
        {
            if (version != _pageLoadVersion)
                return;

            _onlineMetadataLoadFailed = true;
            _pageUiState = PluginPageUiState.Failed;
            // Do not wipe _onlinePlugins on hard failure — keep last good store snapshot.
            _availableUpdates.Clear();
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Error fetching online plugins: {ex.Message}", ex);

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
            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace("Abandoned plugin store fetch faulted.", ex);
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

        var isLoading = _loadingIndicator?.Visibility == Visibility.Visible;
        var hasVisiblePlugins = uniquePlugins.Any();
        var hasAnyPlugins = _allPlugins.Any();

        if (_noPluginsMessage != null)
            _noPluginsMessage.Visibility = !isLoading && !hasVisiblePlugins && !hasAnyPlugins ? Visibility.Visible : Visibility.Collapsed;

        if (_noResultsStackPanel != null)
            _noResultsStackPanel.Visibility = !isLoading && !hasVisiblePlugins && hasAnyPlugins ? Visibility.Visible : Visibility.Collapsed;

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
                // List rebuild must NOT force plugin reload (ScanAndLoad) — that freezes the UI.
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
            _resultsCountTextBlock.Visibility = uniquePlugins.Any() ? Visibility.Visible : Visibility.Collapsed;
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

    private async void PluginExtensionsPage_Loaded(object sender, RoutedEventArgs e)
    {
        AttachPageLifecycleSubscriptions();

        // Always paint skeleton + shimmer first — including hot re-entry / page cache hits.
        // Skipping to ShowCachedContentImmediate made skeleton feel first-open-only.
        // Do NOT call SetPluginResourceCultures here — scanning all assemblies freezes UI.
        ShowSkeletonImmediate();
        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Loaded);
        await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Render);

        // Culture apply after first paint, low priority (fire-and-forget).
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
                // No rows yet — keep skeleton for the whole store fetch (also min-hold).
                await FetchOnlinePluginsAsync(showFullSkeleton: true);
            }
        }
        catch (Exception ex)
        {
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage: initial online plugin fetch failed: {ex.Message}", ex);
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
            // ignore dispose races
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

    private void UpdateAllPluginsUI()
    {
        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            ReconcileAvailableUpdatesWithInstalledVersions();

            // Merge online plugins and locally registered plugins
            var allPluginsList = new List<IPlugin>();
            var pluginIds = new HashSet<string>();

            // First add locally installed plugins
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

            // Then add online plugins (using adapters), but skip already installed ones
            if (_onlinePlugins != null && _onlinePlugins.Count > 0)
            {
                foreach (var onlinePlugin in _onlinePlugins)
                {
                    if (!pluginIds.Contains(onlinePlugin.Id))
                    {
                        allPluginsList.Add(new PluginManifestAdapter(onlinePlugin));
                    }
                }
            }

            _allPlugins = allPluginsList;

            RebuildInstalledStateSnapshot(_allPlugins.Select(plugin => plugin.Id));

            UpdateBulkActionButtonsVisibility();

            // Apply current filters and search
            ApplyFilters();

            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage: Found {_allPlugins.Count} total plugins");
                foreach (var plugin in _allPlugins)
                {
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"  - {plugin.Id}: {plugin.Name} (System: {plugin.IsSystemPlugin}, Installed: {IsPluginInstalledForUi(plugin.Id)})");
                }
            }
        }
        catch (Exception ex)
        {
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Error updating plugins UI: {ex.Message}", ex);

            // Ensure "no plugins" message is shown even on error
            if (_noPluginsMessage != null)
            {
                _noPluginsMessage.Visibility = Visibility.Visible;
            }
        }
        finally
        {
            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                var elapsed = Stopwatch.GetElapsedTime(startedAt);
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace(
                    $"PluginExtensionsPage UI rebuild completed in {elapsed.TotalMilliseconds:0} ms. [plugins={_allPlugins.Count}, rows={_pluginViewModels.Count}]");
            }
        }
    }



    /// <summary>
    /// Remove "Plugin" suffix from plugin name
    /// </summary>
    private string RemovePluginSuffix(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        var suffixes = new[] { "Plugin", "plugin", "PLUG-IN", "Plug-in" };
        foreach (var suffix in suffixes)
        {
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(0, name.Length - suffix.Length).Trim();
            }
        }
        return name;
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
            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Failed to resolve UI installed state for {pluginId}: {ex.Message}", ex);

            return false;
        }
    }

    private void ReconcileAvailableUpdatesWithInstalledVersions()
    {
        if (_availableUpdates.Count == 0)
            return;

        var removedCount = _availableUpdates.RemoveAll(update =>
            !IsAvailableUpdateNewerThanInstalled(update.Id, update.Version));

        if (removedCount > 0 && UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage: removed {removedCount} stale plugin update marker(s)");
    }

    private void RemoveAvailableUpdate(string pluginId)
    {
        var removedCount = _availableUpdates.RemoveAll(update =>
            string.Equals(update.Id, pluginId, StringComparison.OrdinalIgnoreCase));

        if (removedCount > 0 && UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage: cleared update marker for {pluginId}");
    }

    private void UpdateSpecificPluginUI(string pluginId)
    {
        try
        {
            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"UpdateSpecificPluginUI called for {pluginId}");
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"  - IsInstalled for UI: {IsPluginInstalledForUi(pluginId)}");
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"  - Available updates: {_availableUpdates.Count}");
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"  - ViewModel count: {_pluginViewModels.Count}");
            }

            // Find corresponding ViewModel and update its status
            var viewModel = _pluginViewModels.FirstOrDefault(vm => vm.PluginId == pluginId);
            if (viewModel != null)
            {
                var isInstalled = IsPluginInstalledForUi(pluginId);
                var updateAvailable = isInstalled && TryGetAvailableUpdate(pluginId, out _);

                if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Found ViewModel for {pluginId}:");
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"  - Current IsInstalled: {viewModel.IsInstalled}");
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"  - New IsInstalled: {isInstalled}");
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"  - UpdateAvailable: {updateAvailable}");
                }

                // Update ViewModel's installation status and available update status
                viewModel.IsInstalled = isInstalled;
                viewModel.SetUpdateAvailable(updateAvailable);

                // If plugin is now installed, refresh capability flags.
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

                if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Updated plugin UI for {pluginId}: Installed={isInstalled}, UpdateAvailable={updateAvailable}");
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"  - ViewModel InstallButtonText after update: {viewModel.InstallButtonText}");
                }

                UpdateBulkActionButtonsVisibility();
            }
            else
            {
                if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"ViewModel not found for {pluginId}, falling back to full UI update");
                }
                    // If existing ViewModel is not found, perform full UI update
                UpdateAllPluginsUI();
            }
        }
        catch (Exception ex)
        {
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Error updating specific plugin UI for {pluginId}: {ex.Message}", ex);
            // Fallback: perform full UI update
            UpdateAllPluginsUI();
        }
    }

    private async void BulkUpdateButton_Click(object sender, RoutedEventArgs e)
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

    private async void BulkInstallButton_Click(object sender, RoutedEventArgs e)
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
    private async void PluginInstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string pluginId)
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

            if (Application.Current.MainWindow is MainWindow mainWindow)
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

        if (Application.Current.MainWindow is MainWindow mainWindow)
            mainWindow.UpdateInstalledPluginsNavigationItems();
    }

    private async void PluginUninstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string pluginId)
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

    private async void PluginConfigureButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not System.Windows.Controls.Button button || button.Tag is not string pluginId)
                return;

            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"PluginConfigureButton_Click called for {pluginId}");

            await OpenPluginConfigurationAsync(pluginId);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(PluginConfigureButton_Click)}: {ex.Message}", ex);
        }
    }

    private async Task OpenPluginConfigurationAsync(string pluginId)
    {
        try
        {
            // Check if plugin is installed
            if (!_pluginManager.IsInstalled(pluginId))
            {
                if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Plugin {pluginId} is not installed, configuration not available");

                SnackbarHelper.Show(Resource.PluginExtensionsPage_PluginNotInstalled, Resource.PluginExtensionsPage_PluginNotInstalledMessage, SnackbarType.Warning);
                return;
            }

            var plugin = await GetRegisteredPluginForUiAsync(pluginId, forceRefresh: true);
            var manifestMetadata = ResolvePluginManifestMetadata(pluginId);
            var capabilities = ResolvePluginCapabilities(plugin, isInstalled: true, pluginId, manifestMetadata);

            if (!capabilities.SupportsSettingsPage)
            {
                if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Plugin {pluginId} does not provide a settings page");

                SnackbarHelper.Show(
                    Resource.PluginExtensionsPage_NoConfiguration,
                    string.Format(
                        Resource.Culture ?? CultureInfo.CurrentUICulture,
                        T("PluginExtensionsPage_NoConfigurationForPluginMessage", "Plugin {0} does not have any configuration options."),
                        plugin?.Name ?? pluginId),
                    SnackbarType.Info);
                return;
            }

            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Opening configuration window for plugin {pluginId}");

            var window = new Windows.Settings.PluginSettingsWindow(pluginId)
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Error opening plugin settings: {ex.Message}", ex);

            SnackbarHelper.Show(Resource.PluginExtensionsPage_OpenFailed, string.Format(Resource.PluginExtensionsPage_OpenFailedMessage, ex.Message), SnackbarType.Error);
        }
    }

    private async void PluginOpenButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not System.Windows.Controls.Button button || button.Tag is not string pluginId)
                return;

            await OpenPluginEntryPointAsync(pluginId);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(PluginOpenButton_Click)}: {ex.Message}", ex);
        }
    }

    private async Task OpenPluginEntryPointAsync(string pluginId)
    {
        try
        {
            // Ensure plugin is installed
            if (!IsPluginInstalledForUi(pluginId))
            {
                SnackbarHelper.Show(Resource.PluginExtensionsPage_PluginNotInstalled, Resource.PluginExtensionsPage_PluginNotInstalledMessage, SnackbarType.Warning);
                return;
            }

            var plugin = await GetRegisteredPluginForUiAsync(pluginId, forceRefresh: true);
            var manifestMetadata = ResolvePluginManifestMetadata(pluginId);
            var capabilities = ResolvePluginCapabilities(plugin, isInstalled: true, pluginId, manifestMetadata);

            if (plugin is null &&
                !capabilities.SupportsOptimizationCategory &&
                await TryRepairInstalledOnlinePluginAsync(pluginId))
            {
                plugin = await GetRegisteredPluginForUiAsync(pluginId, forceRefresh: true);
                manifestMetadata = ResolvePluginManifestMetadata(pluginId);
                capabilities = ResolvePluginCapabilities(plugin, isInstalled: true, pluginId, manifestMetadata);
            }

            if (TryResolvePluginExecutable(pluginId, out var exeFile, out var pluginDir))
            {
                // Run plugin's executable file
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exeFile!,
                    WorkingDirectory = pluginDir!,
                    UseShellExecute = false
                };

                using var process = System.Diagnostics.Process.Start(processInfo);

                SnackbarHelper.Show(
                    Resource.PluginExtensionsPage_RunPlugin,
                    string.Format(
                        Resource.Culture ?? CultureInfo.CurrentUICulture,
                        T("PluginExtensionsPage_RunPluginStartedMessage", "Started {0}.exe"),
                        pluginId),
                    SnackbarType.Info);
            }
            else if (capabilities.SupportsFeaturePage)
            {
                // Navigate to plugin page (if executable file doesn't exist)
                var mainWindow2 = Application.Current.MainWindow as MainWindow;
                if (mainWindow2 != null && mainWindow2.NavigateToPluginPage(pluginId))
                {
                    return;
                }

                SnackbarHelper.Show(
                    T("PluginExtensionsPage_NavigationFailed", "Navigation Failed"),
                    string.Format(
                        Resource.Culture ?? CultureInfo.CurrentUICulture,
                        T("PluginExtensionsPage_FeatureNavigationFailedMessage", "Plugin {0} page could not be opened."),
                        plugin?.Name ?? pluginId),
                    SnackbarType.Warning);
            }
            else if (capabilities.SupportsOptimizationCategory)
            {
                if (!NavigateToPluginOptimizationCategory(pluginId))
                {
                    SnackbarHelper.Show(
                        T("PluginExtensionsPage_NavigationFailed", "Navigation Failed"),
                        string.Format(
                            Resource.Culture ?? CultureInfo.CurrentUICulture,
                            T("PluginExtensionsPage_NavigationFailedMessage", "Plugin {0} optimization category could not be opened."),
                            plugin?.Name ?? pluginId),
                        SnackbarType.Warning);
                }
            }
            else if (capabilities.SupportsSettingsPage)
            {
                await OpenPluginConfigurationAsync(pluginId);
            }
            else
            {
                SnackbarHelper.Show(
                    T("PluginExtensionsPage_NoUi", "No UI"),
                    string.Format(
                        Resource.Culture ?? CultureInfo.CurrentUICulture,
                        plugin is null
                            ? T("PluginExtensionsPage_NotLoadedMessage", "Plugin {0} is installed, but its runtime UI could not be loaded. Restart the app or reinstall the plugin.")
                            : T("PluginExtensionsPage_NoUiMessage", "Plugin {0} does not expose an entry page."),
                        plugin?.Name ?? pluginId),
                    SnackbarType.Info);
            }
        }
        catch (Exception ex)
        {
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Error opening plugin: {ex.Message}", ex);

            SnackbarHelper.Show(Resource.PluginExtensionsPage_OpenFailed, string.Format(Resource.PluginExtensionsPage_OpenFailedMessage, ex.Message), SnackbarType.Error);
        }
    }

    private async Task OpenPluginDefaultActionAsync(string pluginId)
    {
        try
        {
            if (!IsPluginInstalledForUi(pluginId))
            {
                SnackbarHelper.Show(Resource.PluginExtensionsPage_PluginNotInstalled, Resource.PluginExtensionsPage_PluginNotInstalledMessage, SnackbarType.Warning);
                return;
            }

            await OpenPluginEntryPointAsync(pluginId);
        }
        catch (Exception ex)
        {
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Error opening default plugin action: {ex.Message}", ex);
            SnackbarHelper.Show(Resource.PluginExtensionsPage_OpenFailed, string.Format(Resource.PluginExtensionsPage_OpenFailedMessage, ex.Message), SnackbarType.Error);
        }
    }

    private async Task<bool> TryRepairInstalledOnlinePluginAsync(string pluginId)
    {
        var manifest = _onlinePlugins.FirstOrDefault(plugin =>
            string.Equals(plugin.Id, pluginId, StringComparison.OrdinalIgnoreCase));
        if (manifest is null)
            return false;

        try
        {
            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Repairing installed online plugin runtime before opening UI: {pluginId}");

            var success = await _pluginInstallCoordinator.InstallAsync(manifest);
            if (!success)
                return false;

            _pluginIdsReloadedForUi.Remove(pluginId);
            _recentInstalledVersions[pluginId] = manifest.Version;
            RemoveAvailableUpdate(pluginId);
            ReconcileAvailableUpdatesWithInstalledVersions();
            LocalizationHelper.SetPluginResourceCultures();
            UpdateAllPluginsUI();
            return true;
        }
        catch (Exception ex)
        {
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Failed to repair installed online plugin runtime: {pluginId}", ex);
            return false;
        }
    }

    private bool NavigateToPluginOptimizationCategory(string pluginId)
    {
        var mainWindow = Application.Current.MainWindow as MainWindow;
        if (mainWindow == null)
            return false;

        var navigationStore = mainWindow.FindName("_navigationStore") as NavigationStore;
        if (navigationStore == null)
            return false;

        WindowsOptimizationPage.RequestPluginCategoryFocus(pluginId);
        navigationStore.Navigate("windowsOptimization");
        return true;
    }

    private IPlugin? EnsureRegisteredPluginForUi(string pluginId, bool isInstalled)
    {
        if (!isInstalled)
            return null;

        return GetRegisteredPluginForUi(pluginId, reloadIfMissing: true);
    }

    private PluginManifest? ResolvePluginManifestMetadata(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return null;

        if (TryGetAvailableUpdate(pluginId, out var updatePlugin))
            return updatePlugin;

        var onlinePlugin = _onlinePlugins.FirstOrDefault(plugin =>
            string.Equals(plugin.Id, pluginId, StringComparison.OrdinalIgnoreCase));
        if (onlinePlugin is not null)
            return onlinePlugin;

        var metadata = _pluginManager.GetPluginMetadata(pluginId);
        return TryReadInstalledPluginManifest(pluginId, metadata?.FilePath) ??
               PluginUiCapabilityResolver.ReadInstalledManifest(pluginId);
    }

    private IPlugin? GetRegisteredPluginForUi(string pluginId, bool reloadIfMissing)
    {
        var plugin = _pluginManager.GetRegisteredPlugins()
            .FirstOrDefault(p => string.Equals(p.Id, pluginId, StringComparison.OrdinalIgnoreCase));
        if (plugin != null && plugin is not PluginManifestAdapter)
            return plugin;

        if (!reloadIfMissing)
            return plugin;

        TryReloadPluginForUi(pluginId);

        plugin = _pluginManager.GetRegisteredPlugins()
            .FirstOrDefault(p => string.Equals(p.Id, pluginId, StringComparison.OrdinalIgnoreCase));
        return plugin is PluginManifestAdapter ? null : plugin;
    }

    private async Task<IPlugin?> GetRegisteredPluginForUiAsync(string pluginId, bool forceRefresh)
    {
        var plugin = GetRegisteredPluginForUi(pluginId, reloadIfMissing: false);
        if (plugin is not null)
            return plugin;

        if (!forceRefresh)
            return null;

        _pluginIdsReloadedForUi.Add(pluginId);

        try
        {
            await _pluginManager.ScanAndLoadPluginsAsync(forceRefresh: true);
            return await Dispatcher.InvokeAsync(() =>
            {
                LocalizationHelper.SetPluginResourceCultures();
                UpdateSpecificPluginUI(pluginId);

                var loadedPlugin = _pluginManager.GetRegisteredPlugins()
                    .FirstOrDefault(p => string.Equals(p.Id, pluginId, StringComparison.OrdinalIgnoreCase));
                return loadedPlugin is PluginManifestAdapter ? null : loadedPlugin;
            });
        }
        catch (Exception ex)
        {
            _pluginIdsReloadedForUi.Remove(pluginId);
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage failed to load plugin runtime for UI: {pluginId}", ex);
            return null;
        }
    }

    private void TryReloadPluginForUi(string pluginId)
    {
        if (!_pluginIdsReloadedForUi.Add(pluginId))
            return;

        _ = ReloadPluginRuntimeForUiAsync(pluginId);
    }

    private async Task ReloadPluginRuntimeForUiAsync(string pluginId)
    {
        try
        {
            await _pluginManager.ScanAndLoadPluginsAsync();
            await Dispatcher.InvokeAsync(() =>
            {
                LocalizationHelper.SetPluginResourceCultures();
                UpdateSpecificPluginUI(pluginId);

                if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                    UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage reloaded plugin runtime for UI: {pluginId}");
            });
        }
        catch (Exception ex)
        {
            _pluginIdsReloadedForUi.Remove(pluginId);
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage failed to reload plugin runtime for UI: {pluginId}", ex);
        }
    }

    private void EnsurePluginExtensionsNavigationState()
    {
        WindowsOptimizationPage.ClearPendingPluginCategoryFocus();

        var mainWindow = Application.Current.MainWindow as MainWindow;
        if (mainWindow == null)
            return;

        var navigationStore = mainWindow.FindName("_navigationStore") as NavigationStore;
        if (navigationStore?.Current?.PageTag == "pluginExtensions")
            return;

        navigationStore?.Navigate("pluginExtensions");
    }

}
}
