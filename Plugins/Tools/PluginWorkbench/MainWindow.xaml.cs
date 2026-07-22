using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using UniversalDeviceToolkit.Lib.Optimization;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Plugins.SDK;
using PluginHostMode = UniversalDeviceToolkit.Plugins.SDK.PluginHostMode;
using PluginHostContext = UniversalDeviceToolkit.Plugins.SDK.PluginHostContext;
using Microsoft.Win32;
using PluginTooling.Core;

namespace PluginWorkbench;

public partial class MainWindow : Window
{
    private const string PluginConfigurationRootEnvironmentVariable = "LLT_PLUGIN_CONFIG_ROOT";

    private readonly ObservableCollection<PluginListEntry> _plugins = [];
    private readonly ObservableCollection<OptimizationActionRow> _optimizationActions = [];
    private readonly PluginWorkbenchHostContext _hostContext;
    private readonly PluginWorkbenchLaunchOptions _launchOptions;
    private readonly PluginWorkbenchThemeService _themeService;
    private PluginWorkbenchUiState _uiState;

    private PluginWorkbenchSession? _currentSession;
    private string? _currentSourcePath;
    private bool _currentSourceIsArchive;
    private bool _suppressModeSelectionChanged;
    private bool _suppressThemeSelectionChanged;
    private PluginHostMode _lastConfirmedMode = PluginHostMode.Preview;

    public MainWindow(PluginWorkbenchLaunchOptions launchOptions)
    {
        ArgumentNullException.ThrowIfNull(launchOptions);

        InitializeComponent();

        _launchOptions = launchOptions;
        _themeService = new PluginWorkbenchThemeService(ResolveStatePath(launchOptions.RepositoryRoot));
        _uiState = _themeService.LoadState();

        PluginListBox.ItemsSource = _plugins;
        OptimizationListBox.ItemsSource = _optimizationActions;
        RepositoryPathTextBox.Text = ResolveRepositoryRoot(launchOptions.RepositoryRoot);
        _hostContext = new PluginWorkbenchHostContext(
            () => CurrentMode,
            () => CurrentMode == PluginHostMode.RealRuntime,
            () => this,
            TryOpenPluginSettings);

        PluginHostContext.Current = _hostContext;
        UniversalDeviceToolkit.Plugins.Shared.WpfFallbackHelper.ComponentInitializationFailed += (controlType, error) =>
            Dispatcher.BeginInvoke(() =>
                AppendLog($"[fallback] {controlType.Name} fell back to code-built UI: {error.GetType().Name}: {error.Message}"));
        InitializeSelectors();
        RefreshPluginCatalog();
        ApplyModeToHosts();

        Loaded += MainWindow_Loaded;
    }

    private PluginHostMode CurrentMode =>
        ((ModeComboBox.SelectedItem as ComboBoxItem)?.Tag as string) == "RealRuntime"
            ? PluginHostMode.RealRuntime
            : PluginHostMode.Preview;

    private PluginTooling.Core.PluginWorkbenchThemeMode CurrentTheme =>
        ((ThemeComboBox.SelectedItem as ComboBoxItem)?.Tag as string) switch
        {
            "Light" => PluginTooling.Core.PluginWorkbenchThemeMode.Light,
            "Dark" => PluginTooling.Core.PluginWorkbenchThemeMode.Dark,
            _ => PluginTooling.Core.PluginWorkbenchThemeMode.System,
        };

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
        ApplyTheme(CurrentTheme);
        SelectInitialView();

        if (!string.IsNullOrWhiteSpace(_launchOptions.PluginId))
        {
            await LoadPluginByIdAsync(_launchOptions.PluginId);
        }
        }
        catch (Exception ex)
        {
            AppendLog($"[startup] Failed to initialize: {ex.Message}");
            StatusTextBlock.Text = "Startup initialization failed.";
        }
    }

    private void InitializeSelectors()
    {
        _suppressModeSelectionChanged = true;
        ModeComboBox.SelectedIndex = 0;
        _suppressModeSelectionChanged = false;

        _suppressThemeSelectionChanged = true;
        ThemeComboBox.SelectedIndex = (_launchOptions.ThemeMode ?? _uiState.ThemeMode) switch
        {
            PluginTooling.Core.PluginWorkbenchThemeMode.Light => 1,
            PluginTooling.Core.PluginWorkbenchThemeMode.Dark => 2,
            _ => 0,
        };
        _suppressThemeSelectionChanged = false;

        LogExpander.IsExpanded = _launchOptions.ThemeMode is null && _uiState.IsLogExpanded;
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressThemeSelectionChanged)
        {
            return;
        }

        ApplyTheme(CurrentTheme);
    }

    private async void PluginListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (PluginListBox.SelectedItem is PluginListEntry entry)
        {
            try
            {
            await LoadPluginFromBuildEntryAsync(entry);
            }
            catch (Exception ex)
            {
                AppendLog($"[plugin] Double-click load failed: {ex.Message}");
                StatusTextBlock.Text = "Failed to load plugin.";
            }
        }
    }

    private async void LoadSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
        if (PluginListBox.SelectedItem is not PluginListEntry entry)
        {
            StatusTextBlock.Text = "Select a plugin build output first.";
            return;
        }

        await LoadPluginFromBuildEntryAsync(entry);
        }
        catch (Exception ex)
        {
            AppendLog($"[plugin] Load button failed: {ex.Message}");
            StatusTextBlock.Text = "Failed to load plugin.";
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshPluginCatalog();
    }

    private async void BootstrapHostButton_Click(object sender, RoutedEventArgs e)
    {
        BootstrapHostButton.IsEnabled = false;
        try
        {
            await RunHostBootstrapAsync();
            ApplyTheme(CurrentTheme);
        }
        catch (Exception ex)
        {
            AppendLog($"[bootstrap] Error: {ex.Message}");
            StatusTextBlock.Text = "Bootstrap failed.";
        }
        finally
        {
            BootstrapHostButton.IsEnabled = true;
        }
    }

    private async void OpenZipButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
        var dialog = new OpenFileDialog
        {
            Filter = "ZIP files (*.zip)|*.zip|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            Title = "Select plugin ZIP package"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        await LoadPluginAsync(
            $"ZIP package: {Path.GetFileName(dialog.FileName)}",
            dialog.FileName,
            isArchive: true,
            () => PluginWorkbenchSession.LoadFromArchiveAsync(dialog.FileName, CurrentMode));
        }
        catch (Exception ex)
        {
            AppendLog($"[plugin] ZIP open failed: {ex.Message}");
            StatusTextBlock.Text = "Failed to open ZIP package.";
        }
    }

    private async void ReloadCurrentButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
        await ReloadCurrentAsync();
        }
        catch (Exception ex)
        {
            AppendLog($"[plugin] Reload failed: {ex.Message}");
            StatusTextBlock.Text = "Failed to reload plugin.";
        }
    }

    private async void ModeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
        _suppressModeSelectionChanged = true;
        ModeComboBox.SelectedIndex = CurrentMode == PluginHostMode.RealRuntime ? 0 : 1;
        _suppressModeSelectionChanged = false;
        await HandleModeSelectionChangedAsync();
        }
        catch (Exception ex)
        {
            _suppressModeSelectionChanged = false;
            AppendLog($"[mode] Toggle failed: {ex.Message}");
        }
    }

    private async void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressModeSelectionChanged)
        {
            return;
        }

        try
        {
        await HandleModeSelectionChangedAsync();
        }
        catch (Exception ex)
        {
            AppendLog($"[mode] Selection change failed: {ex.Message}");
        }
    }

    private async Task HandleModeSelectionChangedAsync()
    {
        var selectedMode = CurrentMode;
        if (selectedMode == PluginHostMode.RealRuntime &&
            _lastConfirmedMode != PluginHostMode.RealRuntime)
        {
            var confirmationAccepted = _launchOptions.AutoAcceptRuntimeConfirmation;
            if (!confirmationAccepted)
            {
                var confirmation = MessageBox.Show(
                    this,
                    "Real Runtime can execute plugin startup hooks and system-changing actions. Continue?",
                    "Enable Real Runtime",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning);
                confirmationAccepted = confirmation == MessageBoxResult.OK;
            }

            if (!confirmationAccepted)
            {
                _suppressModeSelectionChanged = true;
                ModeComboBox.SelectedIndex = 0;
                _suppressModeSelectionChanged = false;
                StatusTextBlock.Text = "Stayed in Preview mode.";
                return;
            }

            AppendLog(_launchOptions.AutoAcceptRuntimeConfirmation
                ? "[mode] Switched to Real Runtime (auto-confirmed)."
                : "[mode] Switched to Real Runtime.");
        }
        else if (selectedMode == PluginHostMode.Preview &&
                 _lastConfirmedMode != PluginHostMode.Preview)
        {
            AppendLog("[mode] Switched to Preview.");
        }

        _lastConfirmedMode = selectedMode;
        UpdateModeChrome();
        ApplyModeToHosts();

        if (_currentSession is not null && !string.IsNullOrWhiteSpace(_currentSourcePath))
        {
            await ReloadCurrentAsync();
        }
    }

    private async void RunOptimizationActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: OptimizationActionRow row })
        {
            return;
        }

        if (_currentSession is null || CurrentMode != PluginHostMode.RealRuntime)
        {
            return;
        }

        var confirmation = MessageBox.Show(
            this,
            $"Execute optimization action '{row.Title}' now?",
            "Confirm Optimization Action",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        if (confirmation != MessageBoxResult.OK)
        {
            AppendLog($"[optimization] Cancelled '{row.Title}'.");
            return;
        }

        try
        {
            AppendLog($"[optimization] Executing '{row.Title}'...");
            await row.Action.ExecuteAsync(CancellationToken.None).ConfigureAwait(true);
            row.AppliedState = await ReadAppliedStateAsync(row.Action).ConfigureAwait(true);
            OptimizationListBox.Items.Refresh();
            AppendLog($"[optimization] Completed '{row.Title}'.");
        }
        catch (Exception ex)
        {
            row.AppliedState = $"Failed: {ex.Message}";
            OptimizationListBox.Items.Refresh();
            AppendLog($"[optimization] Failed '{row.Title}': {ex.Message}");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _uiState = _uiState with
        {
            ThemeMode = CurrentTheme,
            LastView = CurrentView,
            IsLogExpanded = _launchOptions.ThemeMode is null && LogExpander.IsExpanded,
        };
        _themeService.SaveState(_uiState);
        UnloadCurrentSession();
        PluginHostContext.Reset();
        base.OnClosed(e);
    }

    private PluginWorkbenchView CurrentView =>
        MainTabControl.SelectedItem switch
        {
            TabItem item when ReferenceEquals(item, SettingsTabItem) => PluginWorkbenchView.Settings,
            TabItem item when ReferenceEquals(item, OptimizationTabItem) => PluginWorkbenchView.Optimization,
            _ => PluginWorkbenchView.Feature,
        };

    private void ApplyTheme(PluginTooling.Core.PluginWorkbenchThemeMode themeMode)
    {
        var result = _themeService.Apply(themeMode);
        ThemeStateTextBlock.Text = $"Theme: {themeMode}";
        ThemeDetailTextBlock.Text = result.Message;
        ResourceStateTextBlock.Text = result.Success ? "Host resources ready" : "Host resources missing";
        HostResourceWarningBorder.Visibility = result.Success ? Visibility.Collapsed : Visibility.Visible;
        HostResourceWarningTextBlock.Text = result.Message;
    }

    private void UpdateModeChrome()
    {
        ModeStateTextBlock.Text = CurrentMode == PluginHostMode.RealRuntime ? "Real Runtime" : "Preview";
        ModeToggleButton.Content = CurrentMode == PluginHostMode.RealRuntime
            ? "Return to Preview"
            : "Enable Real Runtime";
        OptimizationStateTextBlock.Text = _currentSession?.OptimizationCategory is null
            ? "Not available"
            : CurrentMode == PluginHostMode.RealRuntime
                ? "Execution enabled"
                : "Preview only";
    }

    private void SelectInitialView()
    {
        var requestedView = _launchOptions.InitialView ?? _uiState.LastView;
        MainTabControl.SelectedItem = requestedView switch
        {
            PluginWorkbenchView.Settings => SettingsTabItem,
            PluginWorkbenchView.Optimization => OptimizationTabItem,
            _ => FeatureTabItem,
        };
    }

    private void RefreshPluginCatalog()
    {
        _plugins.Clear();

        var repositoryRoot = RepositoryPathTextBox.Text.Trim();
        if (!Directory.Exists(repositoryRoot))
        {
            return;
        }

        foreach (var pluginDirectory in Directory.EnumerateDirectories(Path.Combine(repositoryRoot, "Plugins"))
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var folderName = Path.GetFileName(pluginDirectory);
            if (folderName is "Shared" or "TestCommon" || folderName.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var manifestPath = ResolveManifestPath(pluginDirectory);
            if (manifestPath is null)
            {
                continue;
            }

            var manifest = ParsePluginManifest(manifestPath);
            var buildDirectory = ResolveBuildDirectory(repositoryRoot, folderName, manifest.Id);
            _plugins.Add(new PluginListEntry(
                folderName,
                manifest.Id,
                manifest.Name,
                manifest.Version,
                manifest.MinLltVersion,
                buildDirectory,
                buildDirectory is not null && Directory.Exists(buildDirectory)));
        }

        StatusTextBlock.Text = $"Found {_plugins.Count} plugin folders.";
    }

    private async Task LoadPluginByIdAsync(string pluginId)
    {
        var entry = _plugins.FirstOrDefault(plugin => string.Equals(plugin.PluginId, pluginId, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            AppendLog($"[session] Requested startup plugin '{pluginId}' was not found.");
            return;
        }

        PluginListBox.SelectedItem = entry;
        await LoadPluginFromBuildEntryAsync(entry);
    }

    private async Task LoadPluginFromBuildEntryAsync(PluginListEntry entry)
    {
        if (!entry.BuildAvailable || string.IsNullOrWhiteSpace(entry.BuildDirectory))
        {
            StatusTextBlock.Text = $"No build output found for {entry.PluginId}. Build the plugin first.";
            return;
        }

        await LoadPluginAsync(
            $"Build output: {entry.PluginId} v{entry.Version}",
            entry.BuildDirectory,
            isArchive: false,
            () => PluginWorkbenchSession.LoadFromBuildOutputAsync(entry.BuildDirectory, CurrentMode));
    }

    private async Task LoadPluginAsync(string sourceLabel, string sourcePath, bool isArchive, Func<Task<PluginWorkbenchSession>> sessionFactory)
    {
        try
        {
            StatusTextBlock.Text = $"Loading {sourceLabel}...";
            UnloadCurrentSession();

            ConfigurePluginConfigurationRoot();
            PluginHostContext.Current = _hostContext;

            var session = await sessionFactory().ConfigureAwait(true);
            _currentSession = session;
            _currentSourcePath = sourcePath;
            _currentSourceIsArchive = isArchive;

            PopulateSession(session, sourceLabel);
            AppendLog($"[session] Loaded plugin '{session.Plugin.Id}' from {sourcePath}");
            StatusTextBlock.Text = $"Loaded {session.Plugin.Name} ({session.Plugin.Id}).";
            CurrentSourceTextBlock.Text = sourceLabel;
        }
        catch (Exception ex)
        {
            AppendLog($"[session] Failed to load {sourceLabel}: {ex}");
            StatusTextBlock.Text = $"Failed to load {sourceLabel}.";
            CurrentSourceTextBlock.Text = "Load failed";
        }
    }

    private async Task ReloadCurrentAsync()
    {
        if (string.IsNullOrWhiteSpace(_currentSourcePath))
        {
            return;
        }

        if (_currentSourceIsArchive)
        {
            var archivePath = _currentSourcePath;
            await LoadPluginAsync(
                $"ZIP package: {Path.GetFileName(archivePath)}",
                archivePath,
                true,
                () => PluginWorkbenchSession.LoadFromArchiveAsync(archivePath, CurrentMode));
            return;
        }

        var buildPath = _currentSourcePath;
        await LoadPluginAsync(
            $"Build output: {Path.GetFileName(buildPath)}",
            buildPath,
            false,
            () => PluginWorkbenchSession.LoadFromBuildOutputAsync(buildPath, CurrentMode));
    }

    private void PopulateSession(PluginWorkbenchSession session, string sourceLabel)
    {
        PluginTitleTextBlock.Text = $"{session.Plugin.Name} ({session.Plugin.Id})";
        PluginSubtitleTextBlock.Text = $"{sourceLabel}  |  Mode: {CurrentMode}";
        PluginIdTextBlock.Text = session.Plugin.Id;
        PluginVersionTextBlock.Text = session.PluginVersion;
        PluginMinVersionTextBlock.Text = session.MinimumHostVersion;
        SettingsShellTitleTextBlock.Text = session.Plugin.Name;
        SettingsShellDescriptionTextBlock.Text = session.Plugin.Description;

        FeatureContentHost.Content = session.CreateFeatureContent();
        SettingsContentHost.Content = session.CreateSettingsContent();
        FeatureTabItem.Visibility = FeatureContentHost.Content is null ? Visibility.Collapsed : Visibility.Visible;
        SettingsTabItem.Visibility = SettingsContentHost.Content is null ? Visibility.Collapsed : Visibility.Visible;

        _optimizationActions.Clear();
        if (session.OptimizationCategory is { } category)
        {
            OptimizationTabItem.Visibility = Visibility.Visible;
            foreach (var action in category.Actions)
            {
                _optimizationActions.Add(new OptimizationActionRow(
                    action,
                    HostResourceLookup.Resolve(action.TitleResourceKey),
                    HostResourceLookup.Resolve(action.DescriptionResourceKey),
                    "Checking...",
                    CurrentMode == PluginHostMode.RealRuntime));
            }

            _ = RefreshOptimizationStatesAsync();
        }
        else
        {
            OptimizationTabItem.Visibility = Visibility.Collapsed;
        }

        EnsureSelectedTabIsAvailable();
        UpdateModeChrome();
        ApplyModeToHosts();
    }

    private void EnsureSelectedTabIsAvailable()
    {
        if (MainTabControl.SelectedItem is TabItem selectedItem &&
            selectedItem.Visibility == Visibility.Visible)
        {
            return;
        }

        MainTabControl.SelectedItem = GetFirstVisibleTab();
    }

    private TabItem? GetFirstVisibleTab()
    {
        if (FeatureTabItem.Visibility == Visibility.Visible)
        {
            return FeatureTabItem;
        }

        if (SettingsTabItem.Visibility == Visibility.Visible)
        {
            return SettingsTabItem;
        }

        if (OptimizationTabItem.Visibility == Visibility.Visible)
        {
            return OptimizationTabItem;
        }

        return null;
    }

    private async Task RefreshOptimizationStatesAsync()
    {
        foreach (var row in _optimizationActions)
        {
            row.AppliedState = await ReadAppliedStateAsync(row.Action).ConfigureAwait(true);
        }

        OptimizationListBox.Items.Refresh();
    }

    private static async Task<string> ReadAppliedStateAsync(WindowsOptimizationActionDefinition action)
    {
        if (action.IsAppliedAsync is null)
        {
            return "State probe unavailable";
        }

        try
        {
            return await action.IsAppliedAsync(CancellationToken.None).ConfigureAwait(true)
                ? "Applied"
                : "Not applied";
        }
        catch (Exception ex)
        {
            return $"State probe failed: {ex.Message}";
        }
    }

    private void ApplyModeToHosts()
    {
        var isPreview = CurrentMode == PluginHostMode.Preview;
        FeaturePreviewHintBorder.Visibility = isPreview ? Visibility.Visible : Visibility.Collapsed;
        SettingsPreviewHintBorder.Visibility = isPreview ? Visibility.Visible : Visibility.Collapsed;
        OptimizationPreviewHintBorder.Visibility = isPreview ? Visibility.Visible : Visibility.Collapsed;

        FeatureContentHost.IsEnabled = true;
        SettingsContentHost.IsEnabled = true;
        FeatureContentHost.IsHitTestVisible = !isPreview;
        SettingsContentHost.IsHitTestVisible = !isPreview;

        foreach (var row in _optimizationActions)
        {
            row.CanExecute = !isPreview;
        }

        OptimizationListBox.Items.Refresh();
    }

    private bool TryOpenPluginSettings(string pluginId)
    {
        if (_currentSession is null || !string.Equals(_currentSession.Plugin.Id, pluginId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (SettingsContentHost.Content is null)
        {
            SettingsContentHost.Content = _currentSession.CreateSettingsContent();
            ApplyModeToHosts();
        }

        if (SettingsContentHost.Content is null)
        {
            return false;
        }

        MainTabControl.SelectedItem = SettingsTabItem;
        return true;
    }

    private void UnloadCurrentSession()
    {
        _currentSession?.Dispose();
        _currentSession = null;
        _currentSourcePath = null;
        _currentSourceIsArchive = false;

        FeatureContentHost.Content = null;
        SettingsContentHost.Content = null;
        _optimizationActions.Clear();
        FeatureTabItem.Visibility = Visibility.Visible;
        SettingsTabItem.Visibility = Visibility.Visible;
        OptimizationTabItem.Visibility = Visibility.Visible;

        PluginTitleTextBlock.Text = "No plugin loaded";
        PluginSubtitleTextBlock.Text = "Select a built plugin from the left or open a ZIP package.";
        CurrentSourceTextBlock.Text = "None";
        PluginIdTextBlock.Text = "-";
        PluginVersionTextBlock.Text = "-";
        PluginMinVersionTextBlock.Text = "-";
        SettingsShellTitleTextBlock.Text = "Plugin Settings";
        SettingsShellDescriptionTextBlock.Text = "Host shell preview for plugin settings.";
        OptimizationStateTextBlock.Text = "Not available";
    }

    private void ConfigurePluginConfigurationRoot()
    {
        var repositoryRoot = RepositoryPathTextBox.Text.Trim();
        var configRoot = Path.Combine(repositoryRoot, "Build", "PluginWorkbenchState", CurrentMode.ToString());
        Directory.CreateDirectory(configRoot);
        Environment.SetEnvironmentVariable(PluginConfigurationRootEnvironmentVariable, configRoot);
        AppendLog($"[config] Using isolated plugin config root: {configRoot}");
    }

    private void AppendLog(string line)
    {
        LogTextBox.AppendText(line + Environment.NewLine);
        LogTextBox.ScrollToEnd();
    }

    private static PluginManifest ParsePluginManifest(string manifestPath)
    {
        using var stream = File.OpenRead(manifestPath);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        return new PluginManifest(
            root.GetProperty("id").GetString() ?? Path.GetFileName(Path.GetDirectoryName(manifestPath) ?? manifestPath),
            root.GetProperty("name").GetString() ?? "Unknown Plugin",
            root.GetProperty("version").GetString() ?? "0.0.0",
            root.TryGetProperty("minHostVersion", out var minHostVersion)
                ? minHostVersion.GetString() ?? "0.0.0"
                : root.TryGetProperty("minLLTVersion", out var minVersion) ? minVersion.GetString() ?? "0.0.0" : "0.0.0");
    }

    private static string? ResolveManifestPath(string directory)
    {
        var unified = Path.Combine(directory, "plugin.manifest.json");
        if (File.Exists(unified))
        {
            return unified;
        }

        var legacy = Path.Combine(directory, "plugin.json");
        return File.Exists(legacy) ? legacy : null;
    }

    private static string? ResolveBuildDirectory(string repositoryRoot, string folderName, string pluginId)
    {
        var canonical = Path.Combine(repositoryRoot, "Build", "plugins", $"UniversalDeviceToolkit.Plugins.{folderName}");
        if (Directory.Exists(canonical))
        {
            return canonical;
        }

        var buildRoot = Path.Combine(repositoryRoot, "Build", "plugins");
        if (!Directory.Exists(buildRoot))
        {
            return null;
        }

        foreach (var directory in Directory.EnumerateDirectories(buildRoot))
        {
            var manifestPath = ResolveManifestPath(directory);
            if (manifestPath is null)
            {
                continue;
            }

            var manifest = ParsePluginManifest(manifestPath);
            if (string.Equals(manifest.Id, pluginId, StringComparison.OrdinalIgnoreCase))
            {
                return directory;
            }
        }

        return null;
    }

    private static string ResolveRepositoryRoot(string? repositoryRoot)
    {
        if (!string.IsNullOrWhiteSpace(repositoryRoot) && Directory.Exists(repositoryRoot))
        {
            return repositoryRoot;
        }

        return DetectRepositoryRoot();
    }

    private static string DetectRepositoryRoot()
    {
        var candidates = new[]
        {
            Environment.CurrentDirectory,
            AppContext.BaseDirectory
        };

        foreach (var candidate in candidates)
        {
            var current = new DirectoryInfo(candidate);
            for (var i = 0; i < 8 && current is not null; i++)
            {
                if (File.Exists(Path.Combine(current.FullName, "UniversalDeviceToolkit-Plugins.sln")) &&
                    Directory.Exists(Path.Combine(current.FullName, "Plugins")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }
        }

        return Environment.CurrentDirectory;
    }

    private static string ResolveStatePath(string? repositoryRoot)
    {
        var resolvedRoot = ResolveRepositoryRoot(repositoryRoot);
        return Path.Combine(resolvedRoot, "Build", "PluginWorkbenchState", "ui-state.json");
    }

    private sealed record PluginManifest(string Id, string Name, string Version, string MinLltVersion);

    private sealed record PluginListEntry(
        string FolderName,
        string PluginId,
        string Name,
        string Version,
        string MinimumVersion,
        string? BuildDirectory,
        bool BuildAvailable)
    {
        public string DisplayText => BuildAvailable
            ? $"{Name} ({PluginId})  v{Version}"
            : $"{Name} ({PluginId})  v{Version}  [build missing]";
    }

    private sealed class OptimizationActionRow
    {
        public OptimizationActionRow(
            WindowsOptimizationActionDefinition action,
            string title,
            string description,
            string appliedState,
            bool canExecute)
        {
            Action = action;
            Title = title;
            Description = description;
            AppliedState = appliedState;
            CanExecute = canExecute;
        }

        public WindowsOptimizationActionDefinition Action { get; }
        public string Title { get; }
        public string Description { get; }
        public string AppliedState { get; set; }
        public bool CanExecute { get; set; }
    }

    private async Task RunHostBootstrapAsync()
    {
        var repositoryRoot = RepositoryPathTextBox.Text.Trim();
        if (!Directory.Exists(repositoryRoot))
        {
            AppendLog("[bootstrap] Repository root does not exist.");
            return;
        }

        var scriptPath = Path.Combine(repositoryRoot, "Scripts", "ensure-host-dependencies.ps1");
        if (!File.Exists(scriptPath))
        {
            AppendLog($"[bootstrap] Script not found: {scriptPath}");
            return;
        }

        var shell = OperatingSystem.IsWindows() ? "powershell" : "pwsh";
        var startInfo = new ProcessStartInfo
        {
            FileName = shell,
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            AppendLog("[bootstrap] Failed to launch host bootstrap process.");
            return;
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            AppendLog(stdout.Trim());
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            AppendLog(stderr.Trim());
        }

        AppendLog(process.ExitCode == 0
            ? "[bootstrap] Host dependencies ready."
            : $"[bootstrap] Failed with exit code {process.ExitCode}.");
    }
}