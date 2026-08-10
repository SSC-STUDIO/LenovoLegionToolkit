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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System.Collections.ObjectModel;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Controls.Loading;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Windows;
using PluginConstants = UniversalDeviceToolkit.Lib.Plugins.PluginConstants;
using NavigationItem = UniversalDeviceToolkit.Avalonia.Controls.Custom.NavigationItem;
using NavigationStore = UniversalDeviceToolkit.Avalonia.Controls.Custom.NavigationStore;
using PluginManifest = UniversalDeviceToolkit.Lib.Plugins.PluginManifest;

namespace UniversalDeviceToolkit.Avalonia.Pages;

[LoadingChromeOwner(LoadingChromeOwnership.Page, delayMilliseconds: 0, minimumVisibleMilliseconds: 520)]
public partial class PluginExtensionsPage : global::Avalonia.Controls.UserControl, ILoadingChromeOwner
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
    private bool _onlineMetadataLoadCompleted = false;
    private bool _onlineMetadataLoadFailed = false;
    private readonly Dictionary<string, string> _recentInstalledVersions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _installedStateSnapshot = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _pluginIdsReloadedForUi = new(StringComparer.OrdinalIgnoreCase);

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

        // AVALONIA: removed Title assignment �?Avalonia UserControl has no Title property
        // (page title is rendered via the localized _titleTextBlock below).
        var titleTextBlock = _titleTextBlock;
        if (titleTextBlock != null)
        {
            titleTextBlock.Text = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_Title", "Plugin Extensions", Resource.Culture);
        }

        var descriptionTextBlock = _descriptionTextBlock;
        if (descriptionTextBlock != null)
        {
            descriptionTextBlock.Text = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_Description", "Install and manage plugins to extend functionality", Resource.Culture);
        }

        if (_bulkInstallButton != null)
        {
            _bulkInstallButton.Content = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_InstallAll", "Install All", Resource.Culture);
            ToolTip.SetTip(_bulkInstallButton, LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_InstallAllTooltip", "Install all available plugins", Resource.Culture));
        }

        if (_bulkImportButton != null)
        {
            _bulkImportButton.Content = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_ImportFromFiles", "Import from Files", Resource.Culture);
            ToolTip.SetTip(_bulkImportButton, LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_BulkImportTooltip", "Import plugins from local ZIP files", Resource.Culture));
            _bulkImportButton.IsVisible = true;
        }

        UpdateSummaryMetrics();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is UniversalDeviceToolkit.Avalonia.Controls.TextBox textBox)
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

    private async void PluginConfigureButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not UniversalDeviceToolkit.Avalonia.Controls.Button button || button.Tag is not string pluginId)
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

            var window = new Windows.Settings.PluginSettingsWindow(pluginId);
            var owner = TopLevel.GetTopLevel(this) as Window ?? UdtAppContext.MainWindow;
            if (owner is not null)
                window.ShowDialog(owner);
        }
        catch (Exception ex)
        {
            UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Error opening plugin settings: {ex.Message}", ex);

            SnackbarHelper.Show(Resource.PluginExtensionsPage_OpenFailed, string.Format(Resource.PluginExtensionsPage_OpenFailedMessage, ex.Message), SnackbarType.Error);
        }
    }

    private async void PluginOpenButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not UniversalDeviceToolkit.Avalonia.Controls.Button button || button.Tag is not string pluginId)
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
                var mainWindow2 = TopLevel.GetTopLevel(this) as MainWindow;
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
        var mainWindow = TopLevel.GetTopLevel(this) as MainWindow;
        if (mainWindow == null)
            return false;

        var navigationStore = (mainWindow as INameScope)?.Find("_navigationStore") as NavigationStore;
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
            return await Dispatcher.UIThread.InvokeAsync<IPlugin?>(() =>
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
            await Dispatcher.UIThread.InvokeAsync(() =>
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

        var mainWindow = TopLevel.GetTopLevel(this) as MainWindow;
        if (mainWindow == null)
            return;

        var navigationStore = (mainWindow as INameScope)?.Find("_navigationStore") as NavigationStore;
        if (navigationStore?.Current?.PageTag == "pluginExtensions")
            return;

        navigationStore?.Navigate("pluginExtensions");
    }

}
