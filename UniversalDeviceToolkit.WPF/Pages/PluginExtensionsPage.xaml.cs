using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Windows;
using PluginConstants = LenovoLegionToolkit.Lib.Plugins.PluginConstants;
using Wpf.Ui.Controls;
using NavigationItem = UniversalDeviceToolkit.WPF.Controls.Custom.NavigationItem;
using NavigationStore = UniversalDeviceToolkit.WPF.Controls.Custom.NavigationStore;
using PluginManifest = LenovoLegionToolkit.Lib.Plugins.PluginManifest;

namespace UniversalDeviceToolkit.WPF.Pages
{
public partial class PluginExtensionsPage
{
    private readonly ApplicationSettings _applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
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
    private readonly HashSet<string> _pluginIdsReloadedForUi = new(StringComparer.OrdinalIgnoreCase);
    private bool _isPluginInstallCoordinatorSubscribed;
    private readonly DebounceDispatcher _searchDebouncer = new();

    public PluginExtensionsPage()
    {
        InitializeComponent();
        Loaded += PluginExtensionsPage_Loaded;
        IsVisibleChanged += PluginExtensionsPage_IsVisibleChanged;
        Unloaded += PluginExtensionsPage_Unloaded;
        AttachPluginInstallCoordinator();

        // Subscribe to plugin state changes
        _pluginManager.PluginStateChanged += PluginManager_PluginStateChanged;

        // Initialize ListBox data binding
        if (_pluginsListBox != null)
        {
            _pluginsListBox.ItemsSource = _pluginViewModels;
        }

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
            await FetchOnlinePluginsAsync(forceRefresh: true);
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
        if (_loadingIndicator != null)
            _loadingIndicator.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;

        if (_pluginListPanel != null)
            _pluginListPanel.Visibility = isLoading ? Visibility.Hidden : Visibility.Visible;

        if (!isLoading)
            return;

        if (_noPluginsMessage != null)
            _noPluginsMessage.Visibility = Visibility.Collapsed;

        if (_noResultsStackPanel != null)
            _noResultsStackPanel.Visibility = Visibility.Collapsed;
    }

    private int GetInstallableOnlinePluginCount() =>
        _onlinePlugins.Count(plugin => !IsPluginInstalledForUi(plugin.Id));

    private void UpdateBulkActionButtonsVisibility()
    {
        ReconcileAvailableUpdatesWithInstalledVersions();

        if (_bulkUpdateButton != null)
            _bulkUpdateButton.Visibility = Visibility.Collapsed;

        if (_bulkInstallButton != null)
        {
            _bulkInstallButton.Visibility = Visibility.Collapsed;
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

    private async Task FetchOnlinePluginsAsync(bool forceRefresh = false)
    {
        if (_isLoadingOnlinePlugins)
            return;

        _isLoadingOnlinePlugins = true;
        _onlineMetadataLoadCompleted = false;
        _onlineMetadataLoadFailed = false;

        try
        {
            SetLoadingState(true);

            // Fetch online plugins
            _availableUpdates.Clear();
            _onlinePlugins = await _pluginRepositoryService.FetchAvailablePluginsAsync(forceRefresh);

            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage: Fetched {_onlinePlugins.Count} online plugins");
                foreach (var plugin in _onlinePlugins)
                {
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  - Online: {plugin.Id} v{plugin.Version} (DownloadUrl: {plugin.DownloadUrl})");
                }
            }

            // Refresh the marketplace UI even if the optional update check later fails.
            UpdateAllPluginsUI();

            try
            {
                // Check for plugin updates against actually installed plugin IDs only.
                var installedManifests = BuildInstalledPluginManifestsForUpdateCheck();
                var updates = await _pluginRepositoryService.CheckForUpdatesAsync(installedManifests);
                _availableUpdates = updates;

                if (updates.Count > 0 && LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage: Found {updates.Count} plugin updates");
                    foreach (var update in updates)
                    {
                        LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  - Update available: {update.Id} v{update.Version}");
                    }
                }
            }
            catch (Exception ex)
            {
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage: update check failed after online plugins were loaded: {ex.Message}", ex);
                _availableUpdates.Clear();
            }

            // Refresh once more after update metadata settles so each card gets
            // the latest update badge, version, and changelog state.
            UpdateAllPluginsUI();
            UpdateBulkActionButtonsVisibility();
        }
        catch (Exception ex)
        {
            _onlineMetadataLoadFailed = true;
            _availableUpdates.Clear();
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error fetching online plugins: {ex.Message}", ex);

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
            _onlineMetadataLoadCompleted = true;
            _isLoadingOnlinePlugins = false;
            SetLoadingState(false);

            UpdateAllPluginsUI();
            UpdateBulkActionButtonsVisibility();
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

                if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"UpdatePluginsList: Plugin {plugin.Id} - UI installed check returned {isInstalled}");
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
                if (isInstalled && metadata != null && !string.IsNullOrWhiteSpace(metadata.Version))
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

                var installedManifest = isInstalled ? TryReadInstalledPluginManifest(plugin.Id, metadata?.FilePath) : null;
                var manifestMetadata = installedManifest ?? updatePlugin ?? onlinePlugin;
                var resolvedPlugin = EnsureRegisteredPluginForUi(plugin.Id, isInstalled) ?? plugin;
                var capabilities = ResolvePluginCapabilities(resolvedPlugin, isInstalled, plugin.Id, manifestMetadata);
                var supportsExecutableEntryPoint = isInstalled && TryResolvePluginExecutable(plugin.Id, out _, out _);
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

                    if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                    {
                        LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace(
                            $"UpdatePluginsList: Plugin {plugin.Id} - isInstalled={isInstalled}, pluginType={plugin.GetType().Name}, supportsSettings={capabilities.SupportsSettingsPage}, supportsFeaturePage={capabilities.SupportsFeaturePage}, supportsOptimizationCategory={capabilities.SupportsOptimizationCategory}, supportsExecutableEntryPoint={supportsExecutableEntryPoint}");
                    }

                    existingViewModel.SupportsConfiguration = capabilities.SupportsSettingsPage && _pluginManager.IsInstalled(plugin.Id);
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

                    if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                    {
                        LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace(
                            $"UpdatePluginsList: Plugin {plugin.Id} - isInstalled={isInstalled}, pluginType={plugin.GetType().Name}, supportsSettings={capabilities.SupportsSettingsPage}, supportsFeaturePage={capabilities.SupportsFeaturePage}, supportsOptimizationCategory={capabilities.SupportsOptimizationCategory}, supportsExecutableEntryPoint={supportsExecutableEntryPoint}");
                    }

                    pluginViewModel.SupportsConfiguration = capabilities.SupportsSettingsPage && _pluginManager.IsInstalled(plugin.Id);
                    pluginViewModel.SupportsFeaturePage = capabilities.SupportsFeaturePage;
                    pluginViewModel.SupportsOptimizationCategory = capabilities.SupportsOptimizationCategory;
                    pluginViewModel.SupportsExecutableEntryPoint = supportsExecutableEntryPoint;

                    _pluginViewModels.Add(pluginViewModel);
                }
            }
            catch (Exception ex)
            {
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Failed to update ViewModel for plugin {plugin.Id}: {ex.Message}", ex);
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
        AttachPluginInstallCoordinator();
        LocalizationHelper.SetPluginResourceCultures();

        if (_hasStartedInitialFetch)
        {
            UpdateAllPluginsUI();
            SyncPluginInstallUi();
            return;
        }

        _hasStartedInitialFetch = true;
        SetLoadingState(true);

        try
        {
            await Task.Delay(100); // Small delay to let UI render first
            await FetchOnlinePluginsAsync();
        }
        catch (Exception ex)
        {
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage: initial online plugin fetch failed: {ex.Message}", ex);
            _onlineMetadataLoadFailed = true;
            _onlineMetadataLoadCompleted = true;
            SetLoadingState(false);
            UpdateAllPluginsUI();
            UpdateBulkActionButtonsVisibility();
        }
    }

    private void PluginExtensionsPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            AttachPluginInstallCoordinator();
            EnsurePluginExtensionsNavigationState();

            // Use Dispatcher to ensure UI updates happen after plugin scanning
            Dispatcher.BeginInvoke(new Action(() =>
            {
                LocalizationHelper.SetPluginResourceCultures();
                UpdateAllPluginsUI();
                SyncPluginInstallUi();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void PluginExtensionsPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _pluginManager.PluginStateChanged -= PluginManager_PluginStateChanged;
        DetachPluginInstallCoordinator();
        IsVisibleChanged -= PluginExtensionsPage_IsVisibleChanged;
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
        try
        {
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

            UpdateBulkActionButtonsVisibility();

            // Apply current filters and search
            ApplyFilters();

            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage: Found {_allPlugins.Count} total plugins");
                foreach (var plugin in _allPlugins)
                {
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  - {plugin.Id}: {plugin.Name} (System: {plugin.IsSystemPlugin}, Installed: {IsPluginInstalledForUi(plugin.Id)})");
                }
            }
        }
        catch (Exception ex)
        {
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error updating plugins UI: {ex.Message}", ex);

            // Ensure "no plugins" message is shown even on error
            if (_noPluginsMessage != null)
            {
                _noPluginsMessage.Visibility = Visibility.Visible;
            }
        }
    }



    /// <summary>
    /// Create plugin icon (colored letters)
    /// </summary>
    private UIElement CreatePluginIconOrLetter(IPlugin plugin)
    {
        var name = plugin.Name;
        if (string.IsNullOrWhiteSpace(name))
            name = plugin.Id;

        var letters = new List<char>();
        foreach (var c in name)
        {
            if (char.IsLetter(c))
            {
                letters.Add(c);
                if (letters.Count >= 2)
                    break;
            }
        }

        var darkColors = new List<SolidColorBrush>
        {
            new SolidColorBrush(Color.FromRgb(30, 41, 59)),
            new SolidColorBrush(Color.FromRgb(51, 65, 85)),
            new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            new SolidColorBrush(Color.FromRgb(30, 58, 138)),
            new SolidColorBrush(Color.FromRgb(44, 62, 80)),
            new SolidColorBrush(Color.FromRgb(52, 73, 94)),
            new SolidColorBrush(Color.FromRgb(47, 79, 79)),
            new SolidColorBrush(Color.FromRgb(39, 60, 117))
        };
        var random = new Random(name.GetHashCode());
        var backgroundColor = darkColors[Math.Abs(random.Next()) % darkColors.Count];
        var cornerRadius = Application.Current?.TryFindResource("CornerRadiusControl") is CornerRadius cr
            ? cr
            : new CornerRadius(12);
        var border = new Border
        {
            Background = backgroundColor,
            CornerRadius = cornerRadius,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        if (letters.Count >= 2)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var firstLetter = new TextBlock
            {
                Text = letters[0].ToString().ToUpperInvariant(),
                FontSize = 48,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };

            var secondLetter = new TextBlock
            {
                Text = letters[1].ToString().ToLowerInvariant(),
                FontSize = 48,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };

            stackPanel.Children.Add(firstLetter);
            stackPanel.Children.Add(secondLetter);
            border.Child = stackPanel;
        }
        else if (letters.Count == 1)
        {
            var letter = new TextBlock
            {
                Text = letters[0].ToString().ToUpperInvariant(),
                FontSize = 64,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White
            };
            border.Child = letter;
        }
        else
        {
            var icon = new Wpf.Ui.Controls.SymbolIcon
            {
                Symbol = SymbolRegular.Apps24,
                FontSize = 64,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            icon.SetResourceReference(Control.ForegroundProperty, "SystemAccentColorBrush");
            border.Child = icon;
        }
        return border;
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

    private bool CheckPluginHasUpdate(string pluginId)
    {
        var plugin = _allPlugins.FirstOrDefault(p => p.Id == pluginId);
        if (plugin == null)
            return false;

        return TryGetAvailableUpdate(pluginId, out _);
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

        var metadata = _pluginManager.GetPluginMetadata(pluginId);
        if (metadata == null || string.IsNullOrWhiteSpace(metadata.Version))
        {
            if (_recentInstalledVersions.TryGetValue(pluginId, out var recentVersion))
                return TryParsePluginVersion(availableVersion, out var recentOnlineVersion) &&
                       TryParsePluginVersion(recentVersion, out var installedFromRecentInstall) &&
                       recentOnlineVersion > installedFromRecentInstall;

            return false;
        }

        if (string.IsNullOrWhiteSpace(availableVersion))
        {
            availableVersion = _onlinePlugins
                .FirstOrDefault(plugin => string.Equals(plugin.Id, pluginId, StringComparison.OrdinalIgnoreCase))
                ?.Version;
        }

        if (!TryParsePluginVersion(availableVersion, out var onlineVersion))
            return false;

        if (!TryParsePluginVersion(metadata.Version, out var installedVersion))
            return true;

        return onlineVersion > installedVersion;
    }

    private static bool TryParsePluginVersion(string? rawVersion, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        if (string.IsNullOrWhiteSpace(rawVersion))
            return false;

        var normalized = rawVersion.Trim();
        if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[1..];

        if (Version.TryParse(normalized, out var parsedVersion))
        {
            version = parsedVersion;
            return true;
        }

        return false;
    }

    private bool IsPluginInstalledForUi(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

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
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Failed to resolve UI installed state for {pluginId}: {ex.Message}", ex);

            return false;
        }
    }

    private void ReconcileAvailableUpdatesWithInstalledVersions()
    {
        if (_availableUpdates.Count == 0)
            return;

        var removedCount = _availableUpdates.RemoveAll(update =>
            !IsAvailableUpdateNewerThanInstalled(update.Id, update.Version));

        if (removedCount > 0 && LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage: removed {removedCount} stale plugin update marker(s)");
    }

    private void RemoveAvailableUpdate(string pluginId)
    {
        var removedCount = _availableUpdates.RemoveAll(update =>
            string.Equals(update.Id, pluginId, StringComparison.OrdinalIgnoreCase));

        if (removedCount > 0 && LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage: cleared update marker for {pluginId}");
    }

    private async void PluginUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string pluginId)
            return;

        try
        {
            var onlinePlugin = _onlinePlugins.FirstOrDefault(p => p.Id == pluginId);
            if (onlinePlugin == null)
            {
                SnackbarHelper.Show(
                    Resource.PluginExtensionsPage_UpdateFailed,
                    T("PluginExtensionsPage_OnlineVersionMissing", "Unable to find online version of plugin"),
                    SnackbarType.Error);
                return;
            }

            var updateButton = this.FindName("PluginUpdateButton") as Wpf.Ui.Controls.Button;
            if (updateButton != null)
            {
                updateButton.IsEnabled = false;
                updateButton.Content = T("PluginExtensionsPage_Updating", "Updating...");
            }

            SnackbarHelper.Show(
                Resource.PluginExtensionsPage_UpdatingPlugin,
                string.Format(
                    Resource.Culture ?? CultureInfo.CurrentUICulture,
                    T("PluginExtensionsPage_UpdatingPluginMessageWithName", "Downloading and updating {0}..."),
                    onlinePlugin.Name),
                SnackbarType.Info);

            await InstallOnlinePluginAsync(onlinePlugin);
        }
        catch (Exception ex)
        {
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error updating plugin: {ex.Message}", ex);

            SnackbarHelper.Show(
                Resource.PluginExtensionsPage_UpdateFailed,
                string.Format(
                    Resource.Culture ?? CultureInfo.CurrentUICulture,
                    T("PluginExtensionsPage_UpdateExceptionMessage", "Error updating plugin: {0}"),
                    ex.Message),
                SnackbarType.Error);
        }
        finally
        {
            var updateButton = this.FindName("PluginUpdateButton") as Wpf.Ui.Controls.Button;
            if (updateButton != null)
            {
                updateButton.IsEnabled = true;
                updateButton.Content = Resource.Update;
            }
        }
    }

    /// <summary>
    /// Convert string to SymbolRegular enum value
    /// </summary>
    private Wpf.Ui.Controls.SymbolRegular GetSymbolFromString(string symbolString)
    {
        if (Enum.TryParse<Wpf.Ui.Controls.SymbolRegular>(symbolString, out var symbol))
        {
            return symbol;
        }
        return Wpf.Ui.Controls.SymbolRegular.Apps24;
    }

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
            if (plugin is LenovoLegionToolkit.Lib.Plugins.PluginBase pluginBase)
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
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Failed to resolve plugin capability for {plugin.Id}", ex);
        }

        return new PluginUiCapabilities
        {
            SupportsSettingsPage = supportsSettingsPage,
            SupportsFeaturePage = supportsFeaturePage,
            SupportsOptimizationCategory = supportsOptimizationCategory,
        };
    }

    private void UpdatePluginUI(string pluginId)
    {
        // Toolbox and system optimization are now default apps, no longer need updates here
        // Future real plugin system will handle third-party plugins here
    }

    private void UpdateSpecificPluginUI(string pluginId)
    {
        try
        {
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"UpdateSpecificPluginUI called for {pluginId}");
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  - IsInstalled for UI: {IsPluginInstalledForUi(pluginId)}");
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  - Available updates: {_availableUpdates.Count}");
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  - ViewModel count: {_pluginViewModels.Count}");
            }

            // Find corresponding ViewModel and update its status
            var viewModel = _pluginViewModels.FirstOrDefault(vm => vm.PluginId == pluginId);
            if (viewModel != null)
            {
                var isInstalled = IsPluginInstalledForUi(pluginId);
                var updateAvailable = isInstalled && TryGetAvailableUpdate(pluginId, out _);

                if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Found ViewModel for {pluginId}:");
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  - Current IsInstalled: {viewModel.IsInstalled}");
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  - New IsInstalled: {isInstalled}");
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  - UpdateAvailable: {updateAvailable}");
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
                    viewModel.SupportsExecutableEntryPoint = TryResolvePluginExecutable(pluginId, out _, out _);
                }
                else
                {
                    viewModel.SupportsConfiguration = false;
                    viewModel.SupportsFeaturePage = false;
                    viewModel.SupportsOptimizationCategory = false;
                    viewModel.SupportsExecutableEntryPoint = false;
                }

                if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Updated plugin UI for {pluginId}: Installed={isInstalled}, UpdateAvailable={updateAvailable}");
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  - ViewModel InstallButtonText after update: {viewModel.InstallButtonText}");
                }

                UpdateBulkActionButtonsVisibility();
            }
            else
            {
                if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"ViewModel not found for {pluginId}, falling back to full UI update");
                }
                    // If existing ViewModel is not found, perform full UI update
                UpdateAllPluginsUI();
            }
        }
        catch (Exception ex)
        {
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error updating specific plugin UI for {pluginId}: {ex.Message}", ex);
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
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error during bulk update for {update.Id}: {ex.Message}", ex);
                }
            }

            SnackbarHelper.Show(Resource.PluginExtensionsPage_BulkUpdateComplete, Resource.PluginExtensionsPage_BulkUpdateCompleteMessage, SnackbarType.Success);
        }
        catch (Exception ex)
        {
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error in bulk update: {ex.Message}", ex);
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
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error during bulk install for {candidate.Id}: {ex.Message}", ex);
                }
            }

            SnackbarHelper.Show(bulkInstallComplete, string.Format(bulkInstallCompleteMessage, installedCount), SnackbarType.Success);
        }
        catch (Exception ex)
        {
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error in bulk install: {ex.Message}", ex);
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
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"PluginInstallButton_Click called for {pluginId}");
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  - IsInstalled before install: {_pluginManager.IsInstalled(pluginId)}");
            }

            // Check if this is an online plugin installation
            var onlinePlugin = _onlinePlugins.FirstOrDefault(p => p.Id == pluginId);
            if (onlinePlugin != null)
            {
                if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Installing online plugin: {pluginId}");
                }
                await InstallOnlinePluginAsync(onlinePlugin);
                return;
            }

            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Installing local plugin: {pluginId}");
            }

            // If plugin is already installed, uninstall it first to release file locks
            if (_pluginManager.IsInstalled(pluginId))
            {

                if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Plugin {pluginId} is already installed, uninstalling first to release file locks");
                }
                // Stop plugin before uninstallation to release resources
                _pluginManager.StopPlugin(pluginId);
                _pluginManager.UninstallPlugin(pluginId);

                // Wait a moment for the uninstall to complete
                await Task.Delay(1000);
            }

            _pluginManager.InstallPlugin(pluginId);

            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  - IsInstalled after install: {_pluginManager.IsInstalled(pluginId)}");
            }

            await RefreshInstalledPluginUiAfterInstallAsync(pluginId, forceRefreshRuntime: true);
            await ShowInstalledPluginFeedbackAsync(pluginId);
        }
        catch (Exception ex)
        {
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error installing plugin: {ex.Message}", ex);

            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                SnackbarHelper.Show(Resource.PluginExtensionsPage_InstallFailed, string.Format(Resource.PluginExtensionsPage_InstallFailedMessage, ex.Message), SnackbarType.Error);
            }
        }
    }

    private async Task InstallOnlinePluginAsync(PluginManifest manifest, bool navigateToOptimizationCategoryOnSuccess = true)
    {
        if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
        {
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"InstallOnlinePluginAsync started for {manifest.Id}");
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
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error installing online plugin {manifest.Id}: {ex.Message}", ex);

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

        if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"PluginUninstallButton_Click called for {pluginId}");

        try
        {
            // For local plugins, we should ensure any running processes are stopped
            if (_pluginManager.IsInstalled(pluginId))
            {
                if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Stopping plugin {pluginId} before uninstall");

                // Stop the plugin first
                _pluginManager.StopPlugin(pluginId);
            }

            var result = await Task.Run(() => _pluginManager.UninstallPlugin(pluginId));

            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"UninstallPlugin returned: {result}");

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
            UpdateSpecificPluginUI(pluginId);

            SnackbarHelper.Show(Resource.PluginExtensionsPage_UninstallSuccess, Resource.PluginExtensionsPage_UninstallSuccessMessage, SnackbarType.Success);
        }
        catch (Exception ex)
        {
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error uninstalling plugin: {ex.Message}", ex);

            SnackbarHelper.Show(Resource.PluginExtensionsPage_UninstallFailed, string.Format(Resource.PluginExtensionsPage_UninstallFailedMessage, ex.Message), SnackbarType.Error);
        }
    }

    private async void PluginConfigureButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not System.Windows.Controls.Button button || button.Tag is not string pluginId)
                return;

            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"PluginConfigureButton_Click called for {pluginId}");

            await OpenPluginConfigurationAsync(pluginId);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(PluginConfigureButton_Click)}: {ex.Message}", ex);
        }
    }

    private void PluginUpdateInfoButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.DataContext is not PluginViewModel viewModel)
            return;

        if (!viewModel.HasChangelogUrl || string.IsNullOrWhiteSpace(viewModel.Changelog))
            return;

        if (!Uri.TryCreate(viewModel.Changelog, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Rejected changelog URL for plugin {viewModel.PluginId}: invalid or unsupported scheme.");
            SnackbarHelper.Show(Resource.PluginExtensionsPage_OpenFailed, Resource.PluginExtensionsPage_OpenFailedMessage, SnackbarType.Error);
            return;
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error opening changelog link for plugin {viewModel.PluginId}: {ex.Message}", ex);
            SnackbarHelper.Show(Resource.PluginExtensionsPage_OpenFailed, string.Format(Resource.PluginExtensionsPage_OpenFailedMessage, ex.Message), SnackbarType.Error);
        }
    }

    private async Task OpenPluginConfigurationAsync(string pluginId)
    {
        try
        {
            // Check if plugin is installed
            if (!_pluginManager.IsInstalled(pluginId))
            {
                if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Plugin {pluginId} is not installed, configuration not available");

                SnackbarHelper.Show(Resource.PluginExtensionsPage_PluginNotInstalled, Resource.PluginExtensionsPage_PluginNotInstalledMessage, SnackbarType.Warning);
                return;
            }

            var plugin = await GetRegisteredPluginForUiAsync(pluginId, forceRefresh: true);
            var manifestMetadata = ResolvePluginManifestMetadata(pluginId);
            var capabilities = ResolvePluginCapabilities(plugin, isInstalled: true, pluginId, manifestMetadata);

            if (!capabilities.SupportsSettingsPage)
            {
                if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Plugin {pluginId} does not provide a settings page");

                SnackbarHelper.Show(
                    Resource.PluginExtensionsPage_NoConfiguration,
                    string.Format(
                        Resource.Culture ?? CultureInfo.CurrentUICulture,
                        T("PluginExtensionsPage_NoConfigurationForPluginMessage", "Plugin {0} does not have any configuration options."),
                        plugin?.Name ?? pluginId),
                    SnackbarType.Info);
                return;
            }

            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Opening configuration window for plugin {pluginId}");

            var window = new Windows.Settings.PluginSettingsWindow(pluginId)
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error opening plugin settings: {ex.Message}", ex);

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
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error opening plugin: {ex.Message}", ex);

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
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error opening default plugin action: {ex.Message}", ex);
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
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Repairing installed online plugin runtime before opening UI: {pluginId}");

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
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Failed to repair installed online plugin runtime: {pluginId}", ex);
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
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage failed to load plugin runtime for UI: {pluginId}", ex);
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

                if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage reloaded plugin runtime for UI: {pluginId}");
            });
        }
        catch (Exception ex)
        {
            _pluginIdsReloadedForUi.Remove(pluginId);
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage failed to reload plugin runtime for UI: {pluginId}", ex);
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

    private async void PluginPermanentlyDeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string pluginId)
            return;

        try
        {
            var plugin = _pluginManager.GetRegisteredPlugins().FirstOrDefault(p => p.Id == pluginId);
            if (plugin == null)
                return;

            var result = await MessageBoxHelper.ShowAsync(this,
                T("PluginExtensionsPage_PermanentlyDeleteTitle", "Permanently Delete Plugin"),
                string.Format(
                    Resource.Culture ?? CultureInfo.CurrentUICulture,
                    T("PluginExtensionsPage_PermanentlyDeleteConfirmationMessage", "Are you sure you want to permanently delete plugin \"{0}\"?\n\nThis action cannot be undone, plugin files will be permanently deleted."),
                    plugin.Name),
                Resource.Delete,
                Resource.Cancel);

            if (!result)
                return;

            _pluginManager.StopPlugin(pluginId);
            _pluginManager.UninstallPlugin(pluginId);

            var deleted = await _pluginManager.PermanentlyDeletePluginAsync(pluginId);

            UpdateAllPluginsUI();

            if (deleted)
            {
                SnackbarHelper.Show(
                    T("PluginExtensionsPage_PluginDeleted", "Plugin Deleted"),
                    T("PluginExtensionsPage_PluginDeletedMessage", "Plugin has been permanently deleted from your computer."),
                    SnackbarType.Success);
            }
            else
            {
                SnackbarHelper.Show(
                    Resource.PluginExtensionsPage_PluginUninstalled,
                    T("PluginExtensionsPage_PluginUninstalledLockedMessage", "Plugin will be deleted when the program closes (some files were locked)."),
                    SnackbarType.Info);
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error permanently deleting plugin: {ex.Message}", ex);

            SnackbarHelper.Show(
                Resource.PluginExtensionsPage_DeletionFailed,
                string.Format(
                    Resource.Culture ?? CultureInfo.CurrentUICulture,
                    T("PluginExtensionsPage_DeletionExceptionMessage", "Error occurred while deleting plugin: {0}"),
                    ex.Message),
                SnackbarType.Error);
        }
    }

    private async void BulkImportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var zipFilePaths = ResolveBulkImportZipFilePaths();
            if (zipFilePaths.Count > 0)
            {
                SnackbarHelper.Show(Resource.PluginExtensionsPage_ImportProgress, Resource.PluginExtensionsPage_ImportProgress, SnackbarType.Info);

                int importedCount = 0;
                foreach (var zipFilePath in zipFilePaths)
                {
                    try
                    {
                        // Extract and install plugin
                        var result = await ExtractAndInstallPluginAsync(zipFilePath);
                        if (result)
                        {
                            importedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error importing plugin from {zipFilePath}: {ex.Message}", ex);

                        SnackbarHelper.Show(Resource.PluginExtensionsPage_BulkImportFailed,
                            string.Format(Resource.PluginExtensionsPage_BulkImportFailedMessage, Path.GetFileName(zipFilePath), ex.Message), SnackbarType.Error);
                    }
                }

                // Refresh plugins and UI
                await _pluginManager.ScanAndLoadPluginsAsync();
                LocalizationHelper.SetPluginResourceCultures();
                UpdateAllPluginsUI();

                // Show success message
                if (importedCount > 0)
                {
                    SnackbarHelper.Show(
                        string.Format(Resource.Culture ?? CultureInfo.CurrentUICulture, Resource.PluginExtensionsPage_BulkImportSuccess, importedCount),
                        string.Format(Resource.Culture ?? CultureInfo.CurrentUICulture, Resource.PluginExtensionsPage_BulkImportSuccessMessage, importedCount),
                        SnackbarType.Success);
                }
            }
        }
        catch (Exception ex)
        {
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error in bulk import: {ex.Message}", ex);

            SnackbarHelper.Show(Resource.PluginExtensionsPage_BulkImportFailed,
                string.Format(
                    Resource.PluginExtensionsPage_BulkImportFailedMessage,
                    T("PluginExtensionsPage_UnknownSource", "Unknown"),
                    ex.Message),
                SnackbarType.Error);
        }
    }

    private async Task<bool> ExtractAndInstallPluginAsync(string zipFilePath)
    {
        var pluginsDir = GetPluginsDirectory();
        try
        {
            var installationService = new LenovoLegionToolkit.Lib.Plugins.PluginInstallationService(_pluginManager);
            return await installationService.ExtractAndInstallPluginAsync(zipFilePath, pluginsDir);
        }
        catch (Exception ex)
        {
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error installing plugin from {zipFilePath}: {ex.Message}", ex);
            return false;
        }
    }

    private IReadOnlyList<string> ResolveBulkImportZipFilePaths()
    {
        return PromptForBulkImportZipFilePaths();
    }

    private IReadOnlyList<string> PromptForBulkImportZipFilePaths()
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = Resource.PluginExtensionsPage_SelectPluginFiles,
            Filter = T("PluginExtensionsPage_ZipFileFilter", "ZIP Files (*.zip)|*.zip|All Files (*.*)|*.*"),
            Multiselect = true
        };

        var dialogResult = openFileDialog.ShowDialog();

        return dialogResult == true
            ? openFileDialog.FileNames
            : Array.Empty<string>();
    }

    private UIElement LoadPluginIcon(IPlugin plugin)
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var pluginsRootDir = GetPluginsDirectory();
            var iconExtensions = new[] { ".png", ".jpg", ".jpeg", ".ico", ".svg" };
            string? iconPath = null;

            // Try multiple possible plugin directory names
            var possibleDirNames = new[]
            {
                $"LenovoLegionToolkit.Plugins.{plugin.Id}",
                plugin.Id
            };

            // Try multiple possible file icon names
            var possibleIconNames = new[]
            {
                "icon",
                plugin.Id,
                "plugin",
                "logo"
            };

            foreach (var dirName in possibleDirNames)
            {
                var pluginDir = Path.Combine(pluginsRootDir, dirName);
                if (Directory.Exists(pluginDir))
                {
                    if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                        LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Checking plugin directory for icons: {pluginDir}");

                    foreach (var iconName in possibleIconNames)
                    {
                        foreach (var ext in iconExtensions)
                        {
                            var testPath = Path.Combine(pluginDir, $"{iconName}{ext}");
                            if (File.Exists(testPath))
                            {
                                iconPath = testPath;
                                if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Found icon for plugin {plugin.Id}: {iconPath}");
                                break;
                            }
                        }
                        if (iconPath != null)
                            break;
                    }
                    if (iconPath != null)
                        break;
                }
            }

            if (string.IsNullOrEmpty(iconPath))
            {
                if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"No icon file found for plugin {plugin.Id}, using SymbolIcon with icon string: {plugin.Icon}");

                var symbol = GetSymbolFromString(plugin.Icon);
                var icon = new Wpf.Ui.Controls.SymbolIcon
                {
                    Symbol = symbol,
                    FontSize = 24,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                icon.SetResourceReference(Control.ForegroundProperty, "SystemAccentColorBrush");
                return icon;
            }
            else
            {
                var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(iconPath, UriKind.Absolute);
                bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                var image = new System.Windows.Controls.Image
                {
                    Source = bitmapImage,
                    Width = 32,
                    Height = 32,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Stretch = System.Windows.Media.Stretch.Uniform
                };
                return image;
            }
        }
        catch (Exception ex)
        {
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error loading plugin icon for {plugin.Id}: {ex.Message}", ex);

            var icon = new Wpf.Ui.Controls.SymbolIcon
            {
                Symbol = SymbolRegular.Apps24,
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            icon.SetResourceReference(Control.ForegroundProperty, "SystemAccentColorBrush");
            return icon;
        }
    }

    private string GetPluginsDirectory()
    {
        var pluginsDirectory = PluginPaths.GetPluginsDirectory();
        if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Using plugins directory: {pluginsDirectory}");
        return pluginsDirectory;
    }

    private List<PluginManifest> BuildInstalledPluginManifestsForUpdateCheck()
    {
        var manifests = new List<PluginManifest>();

        foreach (var pluginId in _pluginManager.GetInstalledPluginIds().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            _pluginManager.TryGetPlugin(pluginId, out var plugin);
            var metadata = _pluginManager.GetPluginMetadata(pluginId);

            manifests.Add(new PluginManifest
            {
                Id = pluginId,
                Name = plugin?.Name ?? metadata?.Name ?? pluginId,
                Description = plugin?.Description ?? metadata?.Description ?? string.Empty,
                Version = metadata?.Version ?? "0.0.0",
                Icon = plugin?.Icon ?? metadata?.Icon ?? string.Empty,
                IsSystemPlugin = plugin?.IsSystemPlugin ?? metadata?.IsSystemPlugin ?? false
            });
        }

        return manifests;
    }

    private bool TryResolvePluginExecutable(string pluginId, out string? exeFile, out string? workingDirectory)
    {
        var metadata = _pluginManager.GetPluginMetadata(pluginId);
#if DEBUG
        return PluginExecutableResolver.TryResolve(pluginId, metadata?.FilePath, GetPluginsDirectory(), out exeFile, out workingDirectory, allowUnsignedOverride: true);
#else
        return PluginExecutableResolver.TryResolve(pluginId, metadata?.FilePath, GetPluginsDirectory(), out exeFile, out workingDirectory);
#endif
    }

    private PluginManifest? ResolvePluginManifestForDisplay(IPlugin plugin)
    {
        if (plugin is PluginManifestAdapter adapter)
            return adapter.Manifest;

        return ResolvePluginManifestMetadata(plugin.Id);
    }

    private static PluginMetadata CreatePluginDisplayMetadata(IPlugin plugin, PluginManifest? manifest)
    {
        var fallbackName = ResolvePluginManifestText(manifest, static localization => localization.Name, manifest?.Name);
        var fallbackDescription = ResolvePluginManifestText(manifest, static localization => localization.Description, manifest?.Description ?? manifest?.Store?.Description);

        return new PluginMetadata
        {
            Id = plugin.Id,
            Name = string.IsNullOrWhiteSpace(fallbackName) ? plugin.Name : fallbackName,
            Description = string.IsNullOrWhiteSpace(fallbackDescription) ? plugin.Description : fallbackDescription,
            Icon = plugin.Icon,
            IsSystemPlugin = plugin.IsSystemPlugin,
            Dependencies = plugin.Dependencies,
            Tags = manifest?.Tags ?? manifest?.Store?.Tags,
            LocalizedNames = MergeLocalizedStrings(manifest?.Store?.LocalizedNames, manifest?.LocalizedNames),
            LocalizedDescriptions = MergeLocalizedStrings(manifest?.Store?.LocalizedDescriptions, manifest?.LocalizedDescriptions),
            LocalizedTags = MergeLocalizedTags(manifest?.Store?.LocalizedTags, manifest?.LocalizedTags)
        };
    }

    private static IReadOnlyDictionary<string, string>? MergeLocalizedStrings(
        Dictionary<string, string>? secondary,
        Dictionary<string, string>? primary)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (secondary is not null)
        {
            foreach (var pair in secondary)
                result[pair.Key] = pair.Value;
        }

        if (primary is not null)
        {
            foreach (var pair in primary)
                result[pair.Key] = pair.Value;
        }

        return result.Count == 0 ? null : result;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>>? MergeLocalizedTags(
        Dictionary<string, string[]>? secondary,
        Dictionary<string, string[]>? primary)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        if (secondary is not null)
        {
            foreach (var pair in secondary)
                result[pair.Key] = pair.Value;
        }

        if (primary is not null)
        {
            foreach (var pair in primary)
                result[pair.Key] = pair.Value;
        }

        return result.Count == 0 ? null : result;
    }

    private string GetPluginLocalizedName(IPlugin plugin, PluginManifest? manifest)
    {
        var metadata = CreatePluginDisplayMetadata(plugin, manifest);
        return RemovePluginSuffix(metadata.GetDisplayName(Resource.Culture ?? CultureInfo.CurrentUICulture));
    }

    private async Task<string?> AnalyzeAndFixPluginStructureAsync(string extractDir)
    {
        await Task.Yield();

        try
        {
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Analyzing plugin structure in {extractDir}");

            var subDirectories = Directory.GetDirectories(extractDir);
            if (subDirectories.Length == 0)
            {
                // No subdirectories, check if this is already a plugin directory
                var dllFiles = Directory.GetFiles(extractDir, "*.dll", SearchOption.TopDirectoryOnly);
                var pluginDll = dllFiles.FirstOrDefault(f => Path.GetFileName(f).StartsWith("LenovoLegionToolkit.Plugins.", StringComparison.OrdinalIgnoreCase));

                if (pluginDll != null)
                {
                    // Extract plugin ID from DLL name
                    var dllName = Path.GetFileNameWithoutExtension(pluginDll);
                    var pluginId = dllName.Replace("LenovoLegionToolkit.Plugins.", "");

                    if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                        LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  Found plugin directory with DLL: {pluginId}");

                    // Rename extractDir to pluginId
                    var parentDir = Path.GetDirectoryName(extractDir);
                    if (parentDir != null)
                    {
                        var targetDir = Path.Combine(parentDir, pluginId);
                        if (Directory.Exists(targetDir))
                            Directory.Delete(targetDir, true);
                        Directory.Move(extractDir, targetDir);
                        return pluginId;
                    }
                }

                return null;
            }

            // Check for nested structure
            var firstSubDir = subDirectories[0];
            var firstSubDirName = Path.GetFileName(firstSubDir);

            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  Found subdirectory: {firstSubDirName}");

            // Case 1: Single level nesting (e.g., NetworkAcceleration/LenovoLegionToolkit.Plugins.NetworkAcceleration/)
            if (firstSubDirName.StartsWith("LenovoLegionToolkit.Plugins.", StringComparison.Ordinal))
            {
                if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  Detected single-level nesting, flattening...");

                var pluginId = firstSubDirName.Replace("LenovoLegionToolkit.Plugins.", "");

                // Move all contents from nested directory to extractDir
                await MoveDirectoryContentsAsync(firstSubDir, extractDir);

                // Delete the now-empty nested directory
                Directory.Delete(firstSubDir, true);

                if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                    LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  Successfully flattened to plugin: {pluginId}");

                return pluginId;
            }

            // Case 2: Double level nesting (e.g., NetworkAcceleration/NetworkAcceleration/LenovoLegionToolkit.Plugins.NetworkAcceleration/)
            var nestedSubDirs = Directory.GetDirectories(firstSubDir);
            if (nestedSubDirs.Length == 1)
            {
                var nestedSubDir = nestedSubDirs[0];
                var nestedSubDirName = Path.GetFileName(nestedSubDir);

                if (nestedSubDirName.StartsWith("LenovoLegionToolkit.Plugins.", StringComparison.Ordinal))
                {
                    if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                        LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  Detected double-level nesting, flattening...");

                    var pluginId = nestedSubDirName.Replace("LenovoLegionToolkit.Plugins.", "");

                    // Move all contents from deeply nested directory to extractDir
                    await MoveDirectoryContentsAsync(nestedSubDir, extractDir);

                    // Delete the now-empty nested directories
                    Directory.Delete(nestedSubDir, true);
                    Directory.Delete(firstSubDir, true);

                    if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                        LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  Successfully flattened to plugin: {pluginId}");

                    return pluginId;
                }
            }

            // Case 3: Use the subdirectory name as plugin ID
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"  Using subdirectory as plugin ID: {firstSubDirName}");

            return firstSubDirName;
        }
        catch (Exception ex)
        {
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error analyzing plugin structure: {ex.Message}", ex);
            return null;
        }
    }

    private async Task MoveDirectoryContentsAsync(string sourceDir, string targetDir)
    {
        await Task.Run(() =>
        {
            var files = Directory.GetFiles(sourceDir);
            var dirs = Directory.GetDirectories(sourceDir);

            foreach (var file in files)
            {
                var destFile = Path.Combine(targetDir, Path.GetFileName(file));
                if (File.Exists(destFile))
                    File.Delete(destFile);
                File.Move(file, destFile);
            }

            foreach (var dir in dirs)
            {
                var destDir = Path.Combine(targetDir, Path.GetFileName(dir));
                if (Directory.Exists(destDir))
                    Directory.Delete(destDir, true);
                Directory.Move(dir, destDir);
            }
        });
    }

private string GetPluginLocalizedDescription(IPlugin plugin, PluginManifest? manifest)
    {
        var metadata = CreatePluginDisplayMetadata(plugin, manifest);
        return metadata.GetDisplayDescription(Resource.Culture ?? CultureInfo.CurrentUICulture);
    }

    private IReadOnlyList<string> GetPluginLocalizedTags(IPlugin plugin, PluginManifest? manifest)
    {
        var metadata = CreatePluginDisplayMetadata(plugin, manifest);
        return metadata.GetDisplayTags(Resource.Culture ?? CultureInfo.CurrentUICulture);
    }

    private string GetPluginDetailedDescription(PluginManifest? manifest)
    {
        var manifestValue = ResolvePluginManifestText(manifest, static localization => localization.Details, manifest?.Details ?? manifest?.Store?.Details);
        if (!string.IsNullOrWhiteSpace(manifestValue))
            return manifestValue;

        return string.Empty;
    }

    private string GetPluginUsageGuide(PluginManifest? manifest)
    {
        var manifestValue = ResolvePluginManifestText(manifest, static localization => localization.UsageGuide, manifest?.UsageGuide ?? manifest?.Store?.UsageGuide);
        if (!string.IsNullOrWhiteSpace(manifestValue))
            return manifestValue;

        return string.Empty;
    }

    private static string ResolvePluginManifestText(
        PluginManifest? manifest,
        Func<PluginManifestLocalization, string?> selector,
        string? fallback)
    {
        if (manifest is null)
            return fallback ?? string.Empty;

        foreach (var localization in EnumeratePluginLocalizations(manifest))
        {
            var value = selector(localization);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return fallback ?? string.Empty;
    }

    private static IEnumerable<PluginManifestLocalization> EnumeratePluginLocalizations(PluginManifest manifest)
    {
        var activeCulture = Resource.Culture ?? CultureInfo.CurrentUICulture;
        var localizations = MergePluginLocalizations(manifest.Localizations, manifest.Store?.Localizations);
        foreach (var cultureName in EnumerateCultureNames(activeCulture))
        {
            if (localizations.TryGetValue(cultureName, out var localization))
                yield return localization;
        }
    }

    private static Dictionary<string, PluginManifestLocalization> MergePluginLocalizations(
        Dictionary<string, PluginManifestLocalization>? primary,
        Dictionary<string, PluginManifestLocalization>? secondary)
    {
        var result = new Dictionary<string, PluginManifestLocalization>(StringComparer.OrdinalIgnoreCase);

        if (secondary is not null)
        {
            foreach (var pair in secondary)
                result[pair.Key] = pair.Value;
        }

        if (primary is not null)
        {
            foreach (var pair in primary)
                result[pair.Key] = pair.Value;
        }

        return result;
    }

    private static IEnumerable<string> EnumerateCultureNames(CultureInfo culture)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = culture;
        while (current != CultureInfo.InvariantCulture)
        {
            if (seen.Add(current.Name))
                yield return current.Name;

            if (current.Name.Equals("zh-Hans", StringComparison.OrdinalIgnoreCase) && seen.Add("zh"))
                yield return "zh";

            current = current.Parent;
        }

        if (seen.Add("en"))
            yield return "en";
    }

    private static PluginManifest? TryReadInstalledPluginManifest(string pluginId, string? pluginFilePath)
    {
        if (string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(pluginFilePath))
            return null;

        try
        {
            var pluginDirectory = Path.GetDirectoryName(pluginFilePath);
            if (string.IsNullOrWhiteSpace(pluginDirectory) || !Directory.Exists(pluginDirectory))
                return null;

            foreach (var manifestPath in EnumerateInstalledPluginManifestPaths(pluginDirectory))
            {
                try
                {
                    using var stream = File.OpenRead(manifestPath);
                    var manifest = JsonSerializer.Deserialize<PluginManifest>(stream, InstalledPluginManifestJsonOptions);
                    if (manifest is not null && pluginId.Equals(manifest.Id, StringComparison.OrdinalIgnoreCase))
                        return manifest;
                }
                catch (Exception ex)
                {
                    if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                        LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Failed to read plugin manifest '{manifestPath}': {ex.Message}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Failed to locate installed plugin manifest for {pluginId}: {ex.Message}", ex);
        }

        return null;
    }

    private static readonly JsonSerializerOptions InstalledPluginManifestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static IEnumerable<string> EnumerateInstalledPluginManifestPaths(string pluginDirectory)
    {
        yield return Path.Combine(pluginDirectory, "plugin.manifest.json");
        yield return Path.Combine(pluginDirectory, "plugin.json");
        yield return Path.Combine(pluginDirectory, "Plugin.json");
    }


    private async void PluginListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        try
        {
            PluginViewModel? clickedViewModel = null;

            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace("PluginListBox_MouseDoubleClick triggered");

        // Ignore double-clicks that originate from action buttons inside the item template.
        if (e.OriginalSource is DependencyObject source)
        {
            var current = source;
            while (current != null)
            {
                if (current is System.Windows.Controls.Button)
                {
                    if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                        LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace("PluginListBox_MouseDoubleClick ignored because original source is a button");
                    return;
                }

                if (current is FrameworkElement element && element.DataContext is PluginViewModel viewModel)
                {
                    clickedViewModel = viewModel;
                    if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                        LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"PluginListBox_MouseDoubleClick data context resolved: {viewModel.PluginId}");
                    break;
                }

                current = VisualTreeHelper.GetParent(current);
            }
        }

        var selectedViewModel = clickedViewModel ?? _pluginsListBox.SelectedItem as PluginViewModel;
        if (selectedViewModel != null)
        {
            if (!ReferenceEquals(_pluginsListBox.SelectedItem, selectedViewModel))
                _pluginsListBox.SelectedItem = selectedViewModel;

            var isInstalled = IsPluginInstalledForUi(selectedViewModel.PluginId);
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"PluginListBox_MouseDoubleClick target={selectedViewModel.PluginId}, isInstalled={isInstalled}");

            if (isInstalled)
            {
                await OpenPluginDefaultActionAsync(selectedViewModel.PluginId);
            }
            else
            {
                SnackbarHelper.Show(Resource.PluginExtensionsPage_PluginNotInstalled, Resource.PluginExtensionsPage_PluginNotInstalledMessage, SnackbarType.Warning);
            }
        }
        else if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
        {
            LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace("PluginListBox_MouseDoubleClick no target plugin view model resolved");
        }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(PluginListBox_MouseDoubleClick)}: {ex.Message}", ex);
        }
    }

    private void PluginManager_PluginStateChanged(object? sender, PluginEventArgs e)
    {
        // Update UI when plugin state changes (installed/uninstalled)
        Dispatcher.BeginInvoke(() =>
        {
            UpdateSpecificPluginUI(e.PluginId);
            UpdateAllPluginsUI();
        });
    }

    private void PluginDetailsToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string pluginId)
            return;

        var viewModel = _pluginViewModels.FirstOrDefault(vm =>
            string.Equals(vm.PluginId, pluginId, StringComparison.OrdinalIgnoreCase));
        viewModel?.ToggleDetails();
    }

    private void ContextMenu_OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.Tag is string pluginId)
        {
            if (!PathSecurity.IsValidPluginId(pluginId))
                return;

            try
            {
                var pluginsDir = GetPluginsDirectory();
                var metadata = _pluginManager.GetPluginMetadata(pluginId);
                string path;

                if (metadata?.FilePath != null)
                {
                    path = Path.GetDirectoryName(metadata.FilePath) ?? string.Empty;
                }
                else
                {
                    path = Path.Combine(pluginsDir, pluginId);
                }

                if (Directory.Exists(path))
                {
                    using var process = System.Diagnostics.Process.Start("explorer.exe", path);
                }
                else
                {
                    SnackbarHelper.Show(Resource.PluginExtensionsPage_FolderNotFound, Resource.PluginExtensionsPage_FolderNotFoundMessage, SnackbarType.Warning);
                }
            }
            catch (Exception ex)
            {
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error opening plugin folder: {ex.Message}", ex);
            }
        }
    }

    private void ContextMenu_CopyId_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.Tag is string pluginId)
        {
            try
            {
                System.Windows.Clipboard.SetText(pluginId);
                SnackbarHelper.Show(Resource.PluginExtensionsPage_Copied, string.Format(Resource.PluginExtensionsPage_CopiedMessage, pluginId), SnackbarType.Info);
            }
            catch (Exception ex)
            {
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"Error copying plugin ID: {ex.Message}", ex);
            }
        }
    }
}
}

