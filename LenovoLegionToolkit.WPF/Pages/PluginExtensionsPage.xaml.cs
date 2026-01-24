using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Collections.ObjectModel;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Settings;
using LenovoLegionToolkit.WPF.Utils;
using LenovoLegionToolkit.WPF.Windows;
using PluginConstants = LenovoLegionToolkit.Lib.Plugins.PluginConstants;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using NavigationItem = LenovoLegionToolkit.WPF.Controls.Custom.NavigationItem;
using PluginManifest = LenovoLegionToolkit.Lib.Plugins.PluginManifest;

namespace LenovoLegionToolkit.WPF.Pages;
public partial class PluginExtensionsPage
{
    private readonly ApplicationSettings _applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly IPluginManager _pluginManager = IoCContainer.Resolve<IPluginManager>();
    private readonly PluginSettings _pluginSettings = new();
    private readonly PluginRepositoryService _pluginRepositoryService;
    
private string _currentSearchText = string.Empty;
    private string _currentFilter = "All";
    private List<IPlugin> _allPlugins = new();
    private List<PluginManifest> _onlinePlugins = new();
    private List<PluginManifest> _availableUpdates = new();
    private ObservableCollection<PluginViewModel> _pluginViewModels = new();
    private string _currentSelectedPluginId = string.Empty;
    private bool _isRefreshing = false;
    private string _currentDownloadingPluginId = string.Empty;

    public PluginExtensionsPage()
    {
        _pluginRepositoryService = new PluginRepositoryService(_pluginManager);

        InitializeComponent();
        Loaded += PluginExtensionsPage_Loaded;
        IsVisibleChanged += PluginExtensionsPage_IsVisibleChanged;

        // Subscribe to plugin state changes
        _pluginManager.PluginStateChanged += PluginManager_PluginStateChanged;

        // Add keyboard shortcut for resetting plugin states (Ctrl+Shift+R)
        PreviewKeyDown += PluginExtensionsPage_PreviewKeyDown;
        
        // Initialize loading text with multi-language support
        var loadingText = this.FindName("_loadingText") as System.Windows.Controls.TextBlock;
        if (loadingText != null)
        {
            loadingText.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_Loading", Resource.Culture) ?? "Loading plugins...";
        }
        
        // Initialize ListBox data binding
        if (_pluginsListBox != null)
        {
            _pluginsListBox.ItemsSource = _pluginViewModels;
            _pluginsListBox.MouseDoubleClick += PluginListBox_MouseDoubleClick;
        }
        
        // Set page title and text (using dynamic resources to avoid auto-generated resource issues)
        Title = Resource.ResourceManager.GetString("PluginExtensionsPage_Title", Resource.Culture) ?? "Plugin Extensions";
        
        var titleTextBlock = this.FindName("_titleTextBlock") as System.Windows.Controls.TextBlock;
        if (titleTextBlock != null)
        {
            titleTextBlock.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_Title", Resource.Culture) ?? "Plugin Extensions";
        }
        
        var descriptionTextBlock = this.FindName("_descriptionTextBlock") as System.Windows.Controls.TextBlock;
        if (descriptionTextBlock != null)
        {
            descriptionTextBlock.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_Description", Resource.Culture) ?? "Install and manage plugins to extend functionality";
        }
    }
    
    private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.TextBox textBox)
        {
            _currentSearchText = textBox.Text ?? string.Empty;
            ApplyFilters();
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

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        _isRefreshing = true;
        var refreshButton = this.FindName("_refreshButton") as Wpf.Ui.Controls.Button;
        if (refreshButton != null)
        {
            refreshButton.IsEnabled = false;
            refreshButton.Icon = Wpf.Ui.Common.SymbolRegular.ArrowSync24;
        }

        try
        {
            await FetchOnlinePluginsAsync();
        }
        finally
        {
            _isRefreshing = false;
            if (refreshButton != null)
            {
                refreshButton.IsEnabled = true;
                refreshButton.Icon = Wpf.Ui.Common.SymbolRegular.ArrowClockwise24;
            }
        }
    }

    // private async void ImportPluginButton_Click(object sender, RoutedEventArgs e)
    // {
    //     var openFileDialog = new Microsoft.Win32.OpenFileDialog
    //     {
    //         Title = "Select plugin compressed file",
    //         Filter = "Compressed files (*.zip;*.7z;*.rar)|*.zip;*.7z;*.rar|All files (*.*)|*.*",
    //         Multiselect = false
    //     };

    //     var result = openFileDialog.ShowDialog();
    //     if (result != true)
    //         return;

    //     var filePath = openFileDialog.FileName;
    //     if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
    //         return;

    //     await ImportPluginFileAsync(filePath);
    // }

    // private async void ImportPluginLibraryButton_Click(object sender, RoutedEventArgs e)
    // {
    //     var folderDialog = new System.Windows.Forms.FolderBrowserDialog
    //     {
    //         Description = "Select folder containing plugin compressed files",
    //         ShowNewFolderButton = false
    //     };

    //     var result = folderDialog.ShowDialog();
    //     if (result != System.Windows.Forms.DialogResult.OK)
    //         return;

    //     var folderPath = folderDialog.SelectedPath;
    //     if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
    //         return;

    //     var mainWindow = Application.Current.MainWindow as MainWindow;
    //     if (mainWindow == null)
    //         return;

    //     try
    //     {
    //         var importButton = this.FindName("_importPluginLibraryButton") as Wpf.Ui.Controls.Button;
    //         if (importButton != null)
    //         {
    //             importButton.IsEnabled = false;
    //         }

    //         var pluginFiles = Directory.GetFiles(folderPath, "*.zip")
    //             .Concat(Directory.GetFiles(folderPath, "*.7z"))
    //             .Concat(Directory.GetFiles(folderPath, "*.rar"))
    //             .ToArray();

    //         if (pluginFiles.Length == 0)
    //         {
    //             mainWindow.Snackbar.Show("Import failed", "No plugin compressed files found in the selected folder");
    //             if (importButton != null)
    //             {
    //                 importButton.IsEnabled = true;
    //             }
    //             return;
    //         }

    //         var successCount = 0;
    //         var failCount = 0;

    //         foreach (var filePath in pluginFiles)
    //         {
    //             try
    //             {
    //                 await ImportPluginFileAsync(filePath, showNotification: false);
    //                 successCount++;
    //             }
    //             catch (Exception ex)
    //             {
    //                 Lib.Utils.Log.Instance.Trace($"Error importing plugin {Path.GetFileName(filePath)}: {ex.Message}", ex);
    //                 failCount++;
    //             }
    //         }

    //         await Task.Delay(500);
    //         _pluginManager.ScanAndLoadPlugins();
    //         LocalizationHelper.SetPluginResourceCultures();
    //         UpdateAllPluginsUI();

    //         if (failCount == 0)
    //         {
    //             mainWindow.Snackbar.Show("Import successful", $"Successfully imported {successCount} plugin(s)");
    //         }
    //         else
    //         {
    //             mainWindow.Snackbar.Show("Import completed", $"Successfully imported {successCount} plugin(s), failed {failCount} plugin(s)");
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         Lib.Utils.Log.Instance.Trace($"Error importing plugin library: {ex.Message}", ex);
    //         mainWindow?.Snackbar.Show("Import failed", $"Error importing plugin library: {ex.Message}");
    //     }
    //     finally
    //     {
    //         var importButton = this.FindName("_importPluginLibraryButton") as Wpf.Ui.Controls.Button;
    //         if (importButton != null)
    //         {
    //             importButton.IsEnabled = true;
    //         }
    //     }
    // }

    // private async Task ImportPluginFileAsync(string filePath, bool showNotification = true)
    // {
    //     var mainWindow = Application.Current.MainWindow as MainWindow;
    //     if (mainWindow == null)
    //         return;

    //     try
    //     {
    //         var extension = Path.GetExtension(filePath).ToLowerInvariant();
    //         var pluginsDirectory = GetPluginsDirectory();

    //         // Extract to a temporary directory first to analyze structure
    //         var tempExtractDir = Path.Combine(Path.GetTempPath(), $"plugin_import_{Guid.NewGuid():N}");
    //         Directory.CreateDirectory(tempExtractDir);

    //         if (showNotification)
    //         {
    //             mainWindow.Snackbar.Show("Importing plugin", $"Extracting plugin file: {Path.GetFileName(filePath)}");
    //         }

    //         if (extension == ".zip")
    //         {
    //             await Task.Run(() => System.IO.Compression.ZipFile.ExtractToDirectory(filePath, tempExtractDir));
    //         }
    //         else if (extension == ".7z")
    //         {
    //             await Task.Run(() =>
    //             {
    //                 var processStartInfo = new System.Diagnostics.ProcessStartInfo
    //                 {
    //                     FileName = "7z.exe",
    //                     Arguments = $"x \"{filePath}\" -o\"{tempExtractDir}\" -y",
    //                     UseShellExecute = false,
    //                     CreateNoWindow = true
    //                 };
    //                 System.Diagnostics.Process.Start(processStartInfo)?.WaitForExit();
    //             });
    //         }
    //         else if (extension == ".rar")
    //         {
    //             await Task.Run(() =>
    //             {
    //                 var processStartInfo = new System.Diagnostics.ProcessStartInfo
    //                 {
    //                     FileName = "unrar.exe",
    //                     Arguments = $"x \"{filePath}\" \"{tempExtractDir}\" -y",
    //                     UseShellExecute = false,
    //                     CreateNoWindow = true
    //                 };
    //                 System.Diagnostics.Process.Start(processStartInfo)?.WaitForExit();
    //             });
    //         }
    //         else
    //         {
    //             if (showNotification)
    //             {
    //                 mainWindow.Snackbar.Show("Import failed", "Unsupported compressed file format");
    //             }
    //             Directory.Delete(tempExtractDir, true);
    //             return;
    //         }

    //         // Analyze the extracted structure to find the plugin directory
    //         var pluginId = await AnalyzeAndFixPluginStructureAsync(tempExtractDir);
    //         if (string.IsNullOrEmpty(pluginId))
    //         {
    //             if (showNotification)
    //             {
    //                 mainWindow.Snackbar.Show("Import failed", "Unable to recognize plugin structure, please check the compressed package contents");
    //             }
    //             Directory.Delete(tempExtractDir, true);
    //             return;
    //         }

    //         var pluginDirectory = Path.Combine(pluginsDirectory, pluginId);

    //         if (Directory.Exists(pluginDirectory))
    //         {
    //             if (showNotification)
    //             {
    //                 mainWindow.Snackbar.Show("Import failed", $"Plugin directory already exists: {pluginId}");
    //             }
    //             Directory.Delete(tempExtractDir, true);
    //             return;
    //         }

    //         // Move the fixed plugin directory to the plugins folder
    //         Directory.Move(tempExtractDir, pluginDirectory);

    //         await Task.Delay(500);
    //         _pluginManager.ScanAndLoadPlugins();
    //         LocalizationHelper.SetPluginResourceCultures();
    //         UpdateAllPluginsUI();

    //         if (showNotification)
    //         {
    //             mainWindow.Snackbar.Show("Import successful", $"Plugin {pluginId} imported successfully");
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         Lib.Utils.Log.Instance.Trace($"Error importing plugin: {ex.Message}", ex);
    //         if (showNotification)
    //         {
    //             mainWindow?.Snackbar.Show("Import failed", $"Error importing plugin: {ex.Message}");
    //         }
    //     }
    // }

    private async Task FetchOnlinePluginsAsync()
    {
        // Show loading indicator
        var loadingIndicator = this.FindName("_loadingIndicator") as StackPanel;
        var noPluginsMessage = this.FindName("_noPluginsMessage") as StackPanel;
        
        try
        {
            if (loadingIndicator != null)
            {
                loadingIndicator.Visibility = Visibility.Visible;
            }
            
            if (noPluginsMessage != null)
            {
                noPluginsMessage.Visibility = Visibility.Collapsed;
            }
            
            // Fetch online plugins
            _availableUpdates.Clear();
            _onlinePlugins = await _pluginRepositoryService.FetchAvailablePluginsAsync();
            
            if (Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage: Fetched {_onlinePlugins.Count} online plugins");
                foreach (var plugin in _onlinePlugins)
                {
                    Lib.Utils.Log.Instance.Trace($"  - Online: {plugin.Id} v{plugin.Version} (DownloadUrl: {plugin.DownloadUrl})");
                }
            }
            
            // Check for plugin updates
            var installedPlugins = _pluginManager.GetRegisteredPlugins().ToList();
            
            // Convert IPlugin list to PluginManifest list for update checking
            var installedManifests = new List<PluginManifest>();
            foreach (var plugin in installedPlugins)
            {
                var metadata = _pluginManager.GetPluginMetadata(plugin.Id);
                var manifest = new PluginManifest
                {
                    Id = plugin.Id,
                    Name = plugin.Name,
                    Description = plugin.Description,
                    Version = metadata?.Version ?? "0.0.0",
                    Icon = plugin.Icon,
                    IsSystemPlugin = plugin.IsSystemPlugin
                };
                installedManifests.Add(manifest);
            }
            
            var updates = await _pluginRepositoryService.CheckForUpdatesAsync(installedManifests);
            _availableUpdates = updates;
            
            if (updates.Count > 0 && Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage: Found {updates.Count} plugin updates");
                foreach (var update in updates)
                {
                    Lib.Utils.Log.Instance.Trace($"  - Update available: {update.Id} v{update.Version}");
                }
            }
            
            // Refresh UI
            UpdateAllPluginsUI();
            
            // Hide loading indicator
            if (loadingIndicator != null)
            {
                loadingIndicator.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error fetching online plugins: {ex.Message}", ex);
            
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Snackbar.Show("Failed to fetch plugins", $"Unable to get plugin list from store: {ex.Message}");
            }
            
            // Hide loading indicator
            if (loadingIndicator != null)
            {
                loadingIndicator.Visibility = Visibility.Collapsed;
            }
        }
    }
    
    private void ApplyFilters()
    {
        var filteredPlugins = _allPlugins.AsEnumerable();
        
// Apply filter
        filteredPlugins = _currentFilter switch
        {
            "Installed" => filteredPlugins.Where(p => _pluginManager.IsInstalled(p.Id)),
            "NotInstalled" => filteredPlugins.Where(p => !_pluginManager.IsInstalled(p.Id)),
            _ => filteredPlugins
        };
        
        // Apply search
        if (!string.IsNullOrWhiteSpace(_currentSearchText))
        {
            var searchLower = _currentSearchText.ToLowerInvariant();
            filteredPlugins = filteredPlugins.Where(p => 
                p.Name.ToLowerInvariant().Contains(searchLower) ||
                p.Description.ToLowerInvariant().Contains(searchLower) ||
                p.Id.ToLowerInvariant().Contains(searchLower));
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
        
        foreach (var plugin in uniquePlugins)
        {
            try
            {
                var isInstalled = _pluginManager.IsInstalled(plugin.Id);
                var updateAvailable = _availableUpdates.Any(au => au.Id == plugin.Id);
                
                // Get version information
                var metadata = _pluginManager.GetPluginMetadata(plugin.Id);
                var onlinePlugin = _onlinePlugins.FirstOrDefault(op => op.Id == plugin.Id);
                var updatePlugin = updateAvailable ? _availableUpdates.FirstOrDefault(au => au.Id == plugin.Id) : null;
                
                string version = "1.0.0";
                if (updatePlugin != null && !string.IsNullOrWhiteSpace(updatePlugin.Version))
                    version = updatePlugin.Version;
                else if (onlinePlugin != null && !string.IsNullOrWhiteSpace(onlinePlugin.Version))
                    version = onlinePlugin.Version;
                else if (metadata != null && !string.IsNullOrWhiteSpace(metadata.Version))
                    version = metadata.Version;
                
                // Get iconBackground from online plugin store
                var iconBackground = onlinePlugin?.IconBackground ?? updatePlugin?.IconBackground;
                
                // Find existing ViewModel, update if exists, otherwise create new one
                var existingViewModel = _pluginViewModels.FirstOrDefault(vm => vm.PluginId == plugin.Id);
                
                if (existingViewModel != null)
                {
                    // Update existing ViewModel
                    existingViewModel.IsInstalled = isInstalled;
                    existingViewModel.SetUpdateAvailable(updateAvailable);
                    existingViewModel.Version = $"v{version}";
                    
                    // Update icon background if available
                    if (!string.IsNullOrWhiteSpace(iconBackground))
                    {
                        try
                        {
                            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(iconBackground);
                            existingViewModel.IconBackground = new System.Windows.Media.SolidColorBrush(color);
                        }
                        catch
                        {
                            // Ignore parsing errors
                        }
                    }
                    
                    // Check if plugin supports configuration
                    var supportsConfig = false;
                    if (isInstalled && plugin is LenovoLegionToolkit.Plugins.SDK.PluginBase sdkPlugin)
                    {
                        try
                        {
                            var featureExtension = sdkPlugin.GetFeatureExtension();
                            supportsConfig = featureExtension is LenovoLegionToolkit.Plugins.SDK.IPluginPage;
                        }
                        catch
                        {
                            supportsConfig = false;
                        }
                    }
                    existingViewModel.SupportsConfiguration = supportsConfig;
                }
                else
                {
                    // Create new ViewModel
                    var pluginViewModel = new PluginViewModel(plugin, isInstalled, updateAvailable, version, iconBackground);
                    
                    // Check if plugin supports configuration
                    var supportsConfig = false;
                    if (isInstalled && plugin is LenovoLegionToolkit.Plugins.SDK.PluginBase sdkPlugin)
                    {
                        try
                        {
                            var featureExtension = sdkPlugin.GetFeatureExtension();
                            supportsConfig = featureExtension is LenovoLegionToolkit.Plugins.SDK.IPluginPage;
                        }
                        catch
                        {
                            supportsConfig = false;
                        }
                    }
                    pluginViewModel.SupportsConfiguration = supportsConfig;
                    
                    _pluginViewModels.Add(pluginViewModel);
                }
            }
            catch (Exception ex)
            {
                Lib.Utils.Log.Instance.Trace($"Failed to update ViewModel for plugin {plugin.Id}: {ex.Message}", ex);
            }
        }
        
        // Set ListBox data source
        _pluginsListBox.ItemsSource = _pluginViewModels;
        
        // Update results count
        if (_resultsCountTextBlock != null)
        {
            _resultsCountTextBlock.Text = string.Format(Resource.PluginExtensionsPage_FoundPluginsCount, uniquePlugins.Count);
            _resultsCountTextBlock.Visibility = uniquePlugins.Any() ? Visibility.Visible : Visibility.Collapsed;
        }
        
        // Show/hide no plugins message
        if (_noPluginsMessage != null)
        {
            _noPluginsMessage.Visibility = uniquePlugins.Any() ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void PluginExtensionsPage_Loaded(object sender, RoutedEventArgs e)
    {
        LocalizationHelper.SetPluginResourceCultures();
        UpdateAllPluginsUI();
        
        // Auto-fetch online plugins in background
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000); // Small delay to let UI render first
            await Dispatcher.InvokeAsync(async () =>
            {
                await FetchOnlinePluginsAsync();
            });
        });
    }

    private async Task LoadPluginsFromRootDirectoryAsync()
    {
        try
        {
            var rootDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var pluginsDirectory = Path.Combine(rootDirectory, "plugins");
            
            if (!Directory.Exists(pluginsDirectory))
                return;
            
            var pluginDirectories = Directory.GetDirectories(pluginsDirectory);
            if (pluginDirectories.Length == 0)
                return;
            
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null)
                return;
            
            var loadCount = 0;
            foreach (var pluginDir in pluginDirectories)
            {
                var pluginId = Path.GetFileName(pluginDir);
                
                // Check if plugin is already installed
                if (_pluginManager.IsInstalled(pluginId))
                    continue;
                
                // Check if this is a plugin directory (has DLL file)
                var dllFiles = Directory.GetFiles(pluginDir, "*.dll");
                if (dllFiles.Length == 0)
                    continue;
                
                try
                {
                    // Try to load the plugin
                    _pluginManager.ScanAndLoadPlugins();
                    loadCount++;
                    
                    if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    {
                        Lib.Utils.Log.Instance.Trace($"Auto-loaded plugin from root directory: {pluginId}");
                    }
                }
                catch (Exception ex)
                {
                    Lib.Utils.Log.Instance.Trace($"Error auto-loading plugin {pluginId}: {ex.Message}", ex);
                }
            }
            
            if (loadCount > 0)
            {
                await Task.Delay(500);
                UpdateAllPluginsUI();
                
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    Lib.Utils.Log.Instance.Trace($"Auto-loaded {loadCount} plugins from root directory");
                }
            }
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error loading plugins from root directory: {ex.Message}", ex);
        }
    }

    private void PluginExtensionsPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            // Use Dispatcher to ensure UI updates happen after plugin scanning
            Dispatcher.BeginInvoke(new Action(() =>
            {
                LocalizationHelper.SetPluginResourceCultures();
                UpdateAllPluginsUI();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
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
            
            // Apply current filters and search
            ApplyFilters();
            
            if (Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                Lib.Utils.Log.Instance.Trace($"PluginExtensionsPage: Found {_allPlugins.Count} total plugins");
                foreach (var plugin in _allPlugins)
                {
                    Lib.Utils.Log.Instance.Trace($"  - {plugin.Id}: {plugin.Name} (System: {plugin.IsSystemPlugin}, Installed: {_pluginManager.IsInstalled(plugin.Id)})");
                }
            }
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error updating plugins UI: {ex.Message}", ex);
            
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
        var border = new Border
        {
            Background = backgroundColor,
            CornerRadius = new CornerRadius(12),
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
                Text = letters[0].ToString().ToUpper(),
                FontSize = 48,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };

            var secondLetter = new TextBlock
            {
                Text = letters[1].ToString().ToLower(),
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
                Text = letters[0].ToString().ToUpper(),
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

        var metadata = _pluginManager.GetPluginMetadata(pluginId);
        if (metadata == null || string.IsNullOrWhiteSpace(metadata.Version))
            return false;

        var onlinePlugin = _onlinePlugins.FirstOrDefault(p => p.Id == pluginId);
        if (onlinePlugin == null || string.IsNullOrWhiteSpace(onlinePlugin.Version))
            return false;

        if (Version.TryParse(onlinePlugin.Version, out var onlineVersion) &&
            Version.TryParse(metadata.Version, out var installedVersion))
        {
            return onlineVersion > installedVersion;
        }

        return false;
    }

    private async void PluginUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.Button button || button.Tag is not string pluginId)
            return;

        var mainWindow = Application.Current.MainWindow as MainWindow;

        try
        {
            var onlinePlugin = _onlinePlugins.FirstOrDefault(p => p.Id == pluginId);
            if (onlinePlugin == null)
            {
                mainWindow?.Snackbar.Show(Resource.PluginExtensionsPage_UpdateFailed, "Unable to find online version of plugin");
                return;
            }

            var updateButton = this.FindName("PluginUpdateButton") as Wpf.Ui.Controls.Button;
            if (updateButton != null)
            {
                updateButton.IsEnabled = false;
                updateButton.Content = "Updating...";
            }

            mainWindow?.Snackbar.Show(Resource.PluginExtensionsPage_UpdatingPlugin, string.Format(Resource.PluginExtensionsPage_UpdatingPluginMessage, onlinePlugin.Name));

            var downloadProgressPanel = this.FindName("DownloadProgressPanel") as StackPanel;
            if (downloadProgressPanel != null)
            {
                downloadProgressPanel.Visibility = Visibility.Visible;
            }

            var progressBar = this.FindName("DownloadProgressBar") as System.Windows.Controls.ProgressBar;
            var progressText = this.FindName("DownloadProgressText") as TextBlock;
            if (progressBar != null)
            {
                progressBar.Value = 0;
                progressBar.Visibility = Visibility.Visible;
            }

            if (progressText != null)
            {
                progressText.Text = "Preparing download...";
            }

            _currentDownloadingPluginId = pluginId;

            _pluginRepositoryService.DownloadProgressChanged += OnDownloadProgressChanged;

            var success = await _pluginRepositoryService.DownloadAndInstallPluginAsync(onlinePlugin);

            _pluginRepositoryService.DownloadProgressChanged -= OnDownloadProgressChanged;

if (success)
            {
                _pluginManager.ScanAndLoadPlugins();
                LocalizationHelper.SetPluginResourceCultures();
                UpdateAllPluginsUI();

                mainWindow?.Snackbar.Show(Resource.PluginExtensionsPage_UpdateSuccessful, string.Format(Resource.PluginExtensionsPage_UpdateSuccessfulMessage, onlinePlugin.Name, onlinePlugin.Version));
            }
            else
            {
                mainWindow?.Snackbar.Show(Resource.PluginExtensionsPage_UpdateFailed, Resource.PluginExtensionsPage_UpdateFailedMessage);
            }
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error updating plugin: {ex.Message}", ex);

            mainWindow?.Snackbar.Show(Resource.PluginExtensionsPage_UpdateFailed, $"Error updating plugin: {ex.Message}");
        }
        finally
        {
            _currentDownloadingPluginId = string.Empty;

            var updateButton = this.FindName("PluginUpdateButton") as Wpf.Ui.Controls.Button;
            if (updateButton != null)
            {
                updateButton.IsEnabled = true;
                updateButton.Content = "Update";
            }

            var downloadProgressPanel = this.FindName("DownloadProgressPanel") as StackPanel;
            if (downloadProgressPanel != null)
            {
                downloadProgressPanel.Visibility = Visibility.Collapsed;
            }
        }
    }

    /// <summary>
    /// Convert string to SymbolRegular enum value
    /// </summary>
    private Wpf.Ui.Common.SymbolRegular GetSymbolFromString(string symbolString)
    {
        if (Enum.TryParse<Wpf.Ui.Common.SymbolRegular>(symbolString, out var symbol))
        {
            return symbol;
        }
        return Wpf.Ui.Common.SymbolRegular.Apps24;
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
            if (Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                Lib.Utils.Log.Instance.Trace($"UpdateSpecificPluginUI called for {pluginId}");
                Lib.Utils.Log.Instance.Trace($"  - IsInstalled from manager: {_pluginManager.IsInstalled(pluginId)}");
                Lib.Utils.Log.Instance.Trace($"  - Available updates: {_availableUpdates.Count}");
                Lib.Utils.Log.Instance.Trace($"  - ViewModel count: {_pluginViewModels.Count}");
            }
            
            // Find corresponding ViewModel and update its status
            var viewModel = _pluginViewModels.FirstOrDefault(vm => vm.PluginId == pluginId);
            if (viewModel != null)
            {
                var isInstalled = _pluginManager.IsInstalled(pluginId);
                var updateAvailable = _availableUpdates.Any(au => au.Id == pluginId);
                
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    Lib.Utils.Log.Instance.Trace($"Found ViewModel for {pluginId}:");
                    Lib.Utils.Log.Instance.Trace($"  - Current IsInstalled: {viewModel.IsInstalled}");
                    Lib.Utils.Log.Instance.Trace($"  - New IsInstalled: {isInstalled}");
                    Lib.Utils.Log.Instance.Trace($"  - UpdateAvailable: {updateAvailable}");
                }
                
                // Update ViewModel's installation status and available update status
                viewModel.IsInstalled = isInstalled;
                viewModel.SetUpdateAvailable(updateAvailable);
                
                // If plugin is now installed, check if it supports configuration
                if (isInstalled)
                {
                    var plugin = _allPlugins.FirstOrDefault(p => p.Id == pluginId);
                    if (plugin is LenovoLegionToolkit.Plugins.SDK.PluginBase sdkPlugin)
                    {
                        try
                        {
                            var featureExtension = sdkPlugin.GetFeatureExtension();
                            viewModel.SupportsConfiguration = featureExtension is LenovoLegionToolkit.Plugins.SDK.IPluginPage;
                        }
                        catch
                        {
                            viewModel.SupportsConfiguration = false;
                        }
                    }
                }
                
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    Lib.Utils.Log.Instance.Trace($"Updated plugin UI for {pluginId}: Installed={isInstalled}, UpdateAvailable={updateAvailable}");
                    Lib.Utils.Log.Instance.Trace($"  - ViewModel InstallButtonText after update: {viewModel.InstallButtonText}");
                }
            }
            else
            {
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    Lib.Utils.Log.Instance.Trace($"ViewModel not found for {pluginId}, falling back to full UI update");
                }
                    // If existing ViewModel is not found, perform full UI update
                UpdateAllPluginsUI();
            }
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error updating specific plugin UI for {pluginId}: {ex.Message}", ex);
            // Fallback: perform full UI update
            UpdateAllPluginsUI();
        }
    }
    private async void PluginInstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string pluginId)
            return;

        if (Lib.Utils.Log.Instance.IsTraceEnabled)
        {
            Lib.Utils.Log.Instance.Trace($"PluginInstallButton_Click called for {pluginId}");
            Lib.Utils.Log.Instance.Trace($"  - IsInstalled before install: {_pluginManager.IsInstalled(pluginId)}");
        }

        // Check if this is an online plugin installation
        var onlinePlugin = _onlinePlugins.FirstOrDefault(p => p.Id == pluginId);
        if (onlinePlugin != null)
        {
            if (Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                Lib.Utils.Log.Instance.Trace($"Installing online plugin: {pluginId}");
            }
            await InstallOnlinePluginAsync(onlinePlugin);
            return;
        }

        try
        {
            if (Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                Lib.Utils.Log.Instance.Trace($"Installing local plugin: {pluginId}");
            }
            
            // If plugin is already installed, uninstall it first to release file locks
            if (_pluginManager.IsInstalled(pluginId))
            {
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    Lib.Utils.Log.Instance.Trace($"Plugin {pluginId} is already installed, uninstalling first to release file locks");
                }
                _pluginManager.UninstallPlugin(pluginId);
                
                // Wait a moment for the uninstall to complete
                await Task.Delay(1000);
            }
            
            _pluginManager.InstallPlugin(pluginId);
            
            if (Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                Lib.Utils.Log.Instance.Trace($"  - IsInstalled after install: {_pluginManager.IsInstalled(pluginId)}");
            }
            
            // Immediately update specific plugin's UI state
            UpdateSpecificPluginUI(pluginId);
            
            // Show success message
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_InstallSuccess, Resource.PluginExtensionsPage_InstallSuccessMessage);
                
                // Update navigation items to show newly installed plugin
                mainWindow.UpdateInstalledPluginsNavigationItems();
            }
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error installing plugin: {ex.Message}", ex);
            
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_InstallFailed, string.Format(Resource.PluginExtensionsPage_InstallFailedMessage, ex.Message));
            }
        }
    }

    private async Task InstallOnlinePluginAsync(PluginManifest manifest)
    {
        if (Lib.Utils.Log.Instance.IsTraceEnabled)
        {
            Lib.Utils.Log.Instance.Trace($"InstallOnlinePluginAsync started for {manifest.Id}");
        }
        
        var pluginViewModel = _pluginViewModels.FirstOrDefault(p => p.PluginId == manifest.Id);
        
        try
        {
            _currentDownloadingPluginId = manifest.Id;
            
            if (pluginViewModel != null)
            {
                pluginViewModel.IsInstalling = true;
                pluginViewModel.InstallStatusText = "Preparing download...";
                pluginViewModel.InstallProgress = 0;
            }
            
            // If plugin is already installed, uninstall it first to release file locks
            if (_pluginManager.IsInstalled(manifest.Id))
            {
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                {
                    Lib.Utils.Log.Instance.Trace($"Plugin {manifest.Id} is already installed, uninstalling first to release file locks");
                }
                _pluginManager.UninstallPlugin(manifest.Id);
                
                // Wait a moment for the uninstall to complete
                await Task.Delay(1000);
            }
            
            _pluginRepositoryService.DownloadProgressChanged += OnDownloadProgressChanged;
            
            var success = await _pluginRepositoryService.DownloadAndInstallPluginAsync(manifest);
            
            _pluginRepositoryService.DownloadProgressChanged -= OnDownloadProgressChanged;
            
            var mainWindow = Application.Current.MainWindow as MainWindow;
            
            if (success)
            {
                _pluginManager.ScanAndLoadPlugins();
                LocalizationHelper.SetPluginResourceCultures();
                
                // After rescanning plugins, immediately update specific plugin's UI state
                UpdateSpecificPluginUI(manifest.Id);
                
                if (mainWindow != null)
                {
                    mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_InstallSuccess, $"Plugin {manifest.Name} has been successfully installed!");
                }
            }
            else
            {
                if (mainWindow != null)
                {
                    mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_InstallFailed, $"Plugin installation failed, please check network connection and try again later.");
                }
                
                // Reset plugin's UI state
                UpdateSpecificPluginUI(manifest.Id);
            }
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error installing online plugin {manifest.Id}: {ex.Message}", ex);
            
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_InstallFailed, $"Error installing plugin: {ex.Message}");
            }
        }
        finally
        {
            if (pluginViewModel != null)
            {
                pluginViewModel.IsInstalling = false;
            }
            
            // Ensure UI state is reset in all cases
            UpdateSpecificPluginUI(manifest.Id);
            
            _currentDownloadingPluginId = string.Empty;
        }
    }

    private void OnDownloadProgressChanged(object? sender, PluginDownloadProgress progress)
    {
        if (!string.IsNullOrEmpty(_currentDownloadingPluginId) && progress.PluginId != _currentDownloadingPluginId)
            return;
            
        var pluginViewModel = _pluginViewModels.FirstOrDefault(p => p.PluginId == progress.PluginId);
        if (pluginViewModel != null)
        {
            pluginViewModel.InstallProgress = progress.ProgressPercentage;
            
            if (progress.IsCompleted)
            {
                pluginViewModel.InstallStatusText = "Download completed";
            }
            else if (progress.TotalBytes > 0)
            {
                var downloadedMB = progress.BytesDownloaded / 1024.0 / 1024.0;
                var totalMB = progress.TotalBytes / 1024.0 / 1024.0;
                pluginViewModel.InstallStatusText = $"Downloading... {downloadedMB:F1} / {totalMB:F1} MB ({progress.ProgressPercentage:F0}%)";
            }
            else
            {
                pluginViewModel.InstallStatusText = "Downloading...";
            }
        }
    }
    

    private void PluginUninstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.Button button || button.Tag is not string pluginId)
            return;

        try
        {
            _pluginManager.UninstallPlugin(pluginId);
            
            var detailsPanel = this.FindName("PluginDetailsPanel") as Border;
            if (detailsPanel != null)
            {
                detailsPanel.Visibility = Visibility.Collapsed;
            }
            
            // Immediately update specific plugin's UI state
            UpdateSpecificPluginUI(pluginId);
            
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_UninstallSuccess, Resource.PluginExtensionsPage_UninstallSuccessMessage);
            }
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error uninstalling plugin: {ex.Message}", ex);
            
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_UninstallFailed, string.Format(Resource.PluginExtensionsPage_UninstallFailedMessage, ex.Message));
            }
        }
    }



    private void PluginOpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.Button button || button.Tag is not string pluginId)
            return;

        try
        {
            // Ensure plugin is installed
            if (!_pluginManager.IsInstalled(pluginId))
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_PluginNotInstalled, Resource.PluginExtensionsPage_PluginNotInstalledMessage);
                }
                return;
            }

            // Find plugin's executable file
            var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", pluginId);
            var exeFile = Path.Combine(pluginDir, $"{pluginId}.exe");
            
            if (File.Exists(exeFile))
            {
                // Run plugin's executable file
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exeFile,
                    WorkingDirectory = pluginDir,
                    UseShellExecute = false
                };
                
                System.Diagnostics.Process.Start(processInfo);
                
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_RunPlugin, string.Format(Resource.PluginExtensionsPage_RunPluginMessage, pluginId));
                }
            }
            else
            {
                // Navigate to plugin page (if executable file doesn't exist)
                var mainWindow2 = Application.Current.MainWindow as MainWindow;
                if (mainWindow2 != null)
                {
                    // Use NavigationStore to navigate to plugin page
                    var navigationStore = mainWindow2.FindName("_navigationStore") as NavigationStore;
                    if (navigationStore != null)
                    {
                        // Register page tag to plugin ID mapping
                        PluginPageWrapper.RegisterPluginPageTag($"plugin:{pluginId}", pluginId);
                        
                        // Create a temporary navigation item to navigate to plugin page
                        var tempItem = new NavigationItem
                    {
                        PageTag = $"plugin:{pluginId}",
                        PageType = typeof(PluginPageWrapper)
                    };
                    
                        // Navigate to plugin page
                        navigationStore.Navigate(tempItem.PageTag);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error opening plugin: {ex.Message}", ex);
            
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_OpenFailed, string.Format(Resource.PluginExtensionsPage_OpenPluginFailed, ex.Message));
            }
        }
    }

    private async void PluginPermanentlyDeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.Button button || button.Tag is not string pluginId)
            return;

        var plugin = _pluginManager.GetRegisteredPlugins().FirstOrDefault(p => p.Id == pluginId);
        if (plugin == null)
            return;

        // Show confirmation dialog
        var result = await MessageBoxHelper.ShowAsync(this, 
            "Permanently Delete Plugin", 
            $"Are you sure you want to permanently delete plugin \"{plugin.Name}\"?\n\nThis action cannot be undone, plugin files will be permanently deleted.",
            "Delete",
            "Cancel");

        if (!result)
            return;

        // Execute permanent deletion
        try
        {
            _pluginManager.UninstallPlugin(pluginId);
            
            var detailsPanel = this.FindName("PluginDetailsPanel") as Border;
            if (detailsPanel != null)
            {
                var hideStoryboard = this.FindResource("HideDetailsPanel") as Storyboard;
                if (hideStoryboard != null)
                {
                    hideStoryboard.Completed += (s, args) =>
                    {
                        detailsPanel.Visibility = Visibility.Collapsed;
                    };
                    hideStoryboard.Begin();
                }
                else
                {
                    detailsPanel.Visibility = Visibility.Collapsed;
                }
            }
            UpdateAllPluginsUI();
            
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_PluginUninstalled, Resource.PluginExtensionsPage_PluginUninstalledMessage);
            }
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error permanently deleting plugin: {ex.Message}", ex);
            
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_DeletionFailed, string.Format(Resource.PluginExtensionsPage_DeletionFailedMessage, ex.Message));
            }
        }
    }

    private void SetupLanguageSelection(string pluginId)
    {
        var languageComboBox = this.FindName("PluginLanguageComboBox") as ComboBox;
        if (languageComboBox == null)
            return;

        languageComboBox.Items.Clear();
        languageComboBox.Tag = pluginId;

        // Add "Use Application Default" option
        var defaultItem = new ComboBoxItem
        {
            Content = Resource.ResourceManager.GetString("PluginExtensionsPage_LanguageDefault", Resource.Culture) ?? "Use Application Default",
            Tag = (string?)null
        };
        languageComboBox.Items.Add(defaultItem);

        // Add all available languages
        foreach (var culture in LocalizationHelper.Languages)
        {
            var item = new ComboBoxItem
            {
                Content = LocalizationHelper.LanguageDisplayName(culture),
                Tag = culture.Name
            };
            languageComboBox.Items.Add(item);
        }

        // Select current language setting
        var currentCulture = _pluginSettings.GetPluginCulture(pluginId);
        if (currentCulture != null)
        {
            foreach (ComboBoxItem item in languageComboBox.Items)
            {
                if (item.Tag is string cultureName && cultureName == currentCulture.Name)
                {
                    languageComboBox.SelectedItem = item;
                    break;
                }
            }
        }
        else
        {
            languageComboBox.SelectedItem = defaultItem;
        }

        languageComboBox.Visibility = Visibility.Visible;
    }

    private void PluginLanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox comboBox || comboBox.Tag is not string pluginId)
            return;

        if (comboBox.SelectedItem is not ComboBoxItem selectedItem)
            return;

        var cultureName = selectedItem.Tag as string;
        CultureInfo? cultureInfo = null;
        
        if (!string.IsNullOrWhiteSpace(cultureName))
        {
            try
            {
                cultureInfo = new CultureInfo(cultureName);
            }
            catch
            {
                // Invalid culture name, use null (default)
            }
        }

        _pluginSettings.SetPluginCulture(pluginId, cultureInfo);
        
        // Apply language change immediately
        if (cultureInfo != null)
        {
            ApplyPluginLanguage(pluginId, cultureInfo);
        }
        else
        {
            // Use application default
            ApplyPluginLanguage(pluginId, Resource.Culture ?? CultureInfo.CurrentUICulture);
        }
        
        // Refresh plugin list to update language
        UpdateAllPluginsUI();
    }

    private void ApplyPluginLanguage(string pluginId, CultureInfo cultureInfo)
    {
        try
        {
            // Find the plugin assembly and set its Resource.Culture
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var assemblyName = assembly.GetName().Name;
                if (assemblyName != null && assemblyName.StartsWith("LenovoLegionToolkit.Plugins.", StringComparison.OrdinalIgnoreCase))
                {
                    // Check if this is the right plugin by finding a class with the plugin ID
                    var pluginType = assembly.GetTypes()
                        .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                    
                    if (pluginType != null)
                    {
                        try
                        {
                            var pluginInstance = Activator.CreateInstance(pluginType) as IPlugin;
                            if (pluginInstance?.Id == pluginId)
                            {
                                // Found the plugin, now set its Resource.Culture
                                var resourceType = assembly.GetType($"{assemblyName}.Resource");
                                if (resourceType != null)
                                {
                                    var cultureProperty = resourceType.GetProperty("Culture", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                                    cultureProperty?.SetValue(null, cultureInfo);
                                    
                                    if (Lib.Utils.Log.Instance.IsTraceEnabled)
                                        Lib.Utils.Log.Instance.Trace($"Applied language {cultureInfo.Name} to plugin {pluginId}");
                                }
                                break;
                            }
                        }
                        catch
                        {
                            // Continue searching for the right plugin
                        }
                    }
                }
            }
        }
        finally
        {
            // Cleanup or final operations if needed
        }
    }

    private async void BulkImportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Create OpenFileDialog for ZIP files
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = Resource.PluginExtensionsPage_SelectPluginFiles,
                Filter = "ZIP Files (*.zip)|*.zip|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_ImportProgress, Resource.PluginExtensionsPage_ImportProgress);
                }

                int importedCount = 0;
                foreach (var zipFilePath in openFileDialog.FileNames)
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
                        Lib.Utils.Log.Instance.Trace($"Error importing plugin from {zipFilePath}: {ex.Message}", ex);

                        if (mainWindow != null)
                        {
                            mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_BulkImportFailed,
                                string.Format(Resource.PluginExtensionsPage_BulkImportFailedMessage, Path.GetFileName(zipFilePath), ex.Message));
                        }
                    }
                }

                // Refresh plugins and UI
                _pluginManager.ScanAndLoadPlugins();
                UpdateAllPluginsUI();

                // Clear the import status cache
                _currentDownloadingPluginId = string.Empty;

                // Show success message
                if (mainWindow != null && importedCount > 0)
                {
                    mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_BulkImportSuccess,
                        string.Format(Resource.PluginExtensionsPage_BulkImportSuccessMessage, importedCount));
                }
            }
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error in bulk import: {ex.Message}", ex);

            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_BulkImportFailed,
                    string.Format(Resource.PluginExtensionsPage_BulkImportFailedMessage, "Unknown", ex.Message));
            }
        }
    }

    private async Task<bool> ExtractAndInstallPluginAsync(string zipFilePath)
    {
        var pluginsDir = GetPluginsDirectory();
        var tempDir = Path.Combine(Path.GetTempPath(), "LLTPluginImport", Guid.NewGuid().ToString());

        try
        {
            // Create temp directory
            Directory.CreateDirectory(tempDir);

            // Extract ZIP
            ZipFile.ExtractToDirectory(zipFilePath, tempDir);

            // Find plugin directories (should contain a .csproj file)
            var pluginDirs = Directory.GetDirectories(tempDir)
                .Where(dir => Directory.GetFiles(dir, "*.csproj").Any())
                .ToList();

            if (!pluginDirs.Any())
            {
                throw new InvalidOperationException("No valid plugin project found in ZIP file");
            }

            foreach (var pluginDir in pluginDirs)
            {
                var csprojFile = Directory.GetFiles(pluginDir, "*.csproj").First();
                var pluginId = Path.GetFileNameWithoutExtension(csprojFile).Replace("LenovoLegionToolkit.Plugins.", "");

                // Copy to plugins directory
                var targetDir = Path.Combine(pluginsDir, pluginId);
                if (Directory.Exists(targetDir))
                {
                    Directory.Delete(targetDir, true);
                }

                CopyDirectory(pluginDir, targetDir);

                // Try to build the plugin
                var buildResult = await BuildPluginAsync(targetDir);
                if (!buildResult)
                {
                    Lib.Utils.Log.Instance.Trace($"Failed to build plugin {pluginId}");
                    continue;
                }

                // Plugin files copied successfully
                Lib.Utils.Log.Instance.Trace($"Successfully copied plugin {pluginId} to {targetDir}");
                return true;
            }

            return false;
        }
        finally
        {
            // Clean up temp directory
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    private void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            var targetFile = Path.Combine(targetDir, fileName);
            File.Copy(file, targetFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            var targetSubDir = Path.Combine(targetDir, dirName);
            CopyDirectory(dir, targetSubDir);
        }
    }

    private async Task<bool> BuildPluginAsync(string pluginDir)
    {
        try
        {
            var csprojFile = Directory.GetFiles(pluginDir, "*.csproj").First();
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{csprojFile}\" --configuration Release --no-incremental",
                WorkingDirectory = pluginDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
            {
                return false;
            }

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
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
                    if (Lib.Utils.Log.Instance.IsTraceEnabled)
                        Lib.Utils.Log.Instance.Trace($"Checking plugin directory for icons: {pluginDir}");
                    
                    foreach (var iconName in possibleIconNames)
                    {
                        foreach (var ext in iconExtensions)
                        {
                            var testPath = Path.Combine(pluginDir, $"{iconName}{ext}");
                            if (File.Exists(testPath))
                            {
                                iconPath = testPath;
                                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                                    Lib.Utils.Log.Instance.Trace($"Found icon for plugin {plugin.Id}: {iconPath}");
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
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    Lib.Utils.Log.Instance.Trace($"No icon file found for plugin {plugin.Id}, using SymbolIcon with icon string: {plugin.Icon}");
                
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
            if (Lib.Utils.Log.Instance.IsTraceEnabled)
                Lib.Utils.Log.Instance.Trace($"Error loading plugin icon for {plugin.Id}: {ex.Message}", ex);
            
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
        var appBaseDir = AppDomain.CurrentDomain.BaseDirectory;
        
        var possiblePaths = new[]
        {
            Path.Combine(appBaseDir, "build", "plugins"),
            Path.Combine(appBaseDir, "..", "..", "..", "build", "plugins"),
            Path.Combine(appBaseDir, "..", "build", "plugins"),
        };

        foreach (var possiblePath in possiblePaths)
        {
            var fullPath = Path.GetFullPath(possiblePath);
            if (Directory.Exists(fullPath))
            {
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    Lib.Utils.Log.Instance.Trace($"Found plugins directory: {fullPath}");
                return fullPath;
            }
        }

        var defaultPath = Path.Combine(appBaseDir, "build", "plugins");
        Directory.CreateDirectory(defaultPath);
        if (Lib.Utils.Log.Instance.IsTraceEnabled)
            Lib.Utils.Log.Instance.Trace($"Using default plugins directory: {defaultPath}");
        return defaultPath;
    }

    private string GetPluginLocalizedName(IPlugin plugin)
    {
        var resourceName = $"Plugin_Name_{plugin.Id}";
        var property = typeof(Resource).GetProperty(resourceName);
        if (property != null)
        {
            var value = property.GetValue(null) as string;
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }
        return RemovePluginSuffix(plugin.Name);
    }

    private async Task<string?> AnalyzeAndFixPluginStructureAsync(string extractDir)
    {
        try
        {
            if (Lib.Utils.Log.Instance.IsTraceEnabled)
                Lib.Utils.Log.Instance.Trace($"Analyzing plugin structure in {extractDir}");

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
                    
                    if (Lib.Utils.Log.Instance.IsTraceEnabled)
                        Lib.Utils.Log.Instance.Trace($"  Found plugin directory with DLL: {pluginId}");
                    
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

            if (Lib.Utils.Log.Instance.IsTraceEnabled)
                Lib.Utils.Log.Instance.Trace($"  Found subdirectory: {firstSubDirName}");

            // Case 1: Single level nesting (e.g., NetworkAcceleration/LenovoLegionToolkit.Plugins.NetworkAcceleration/)
            if (firstSubDirName.StartsWith("LenovoLegionToolkit.Plugins."))
            {
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    Lib.Utils.Log.Instance.Trace($"  Detected single-level nesting, flattening...");

                var pluginId = firstSubDirName.Replace("LenovoLegionToolkit.Plugins.", "");

                // Move all contents from nested directory to extractDir
                await MoveDirectoryContentsAsync(firstSubDir, extractDir);

                // Delete the now-empty nested directory
                Directory.Delete(firstSubDir, true);

                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    Lib.Utils.Log.Instance.Trace($"  Successfully flattened to plugin: {pluginId}");

                return pluginId;
            }

            // Case 2: Double level nesting (e.g., NetworkAcceleration/NetworkAcceleration/LenovoLegionToolkit.Plugins.NetworkAcceleration/)
            var nestedSubDirs = Directory.GetDirectories(firstSubDir);
            if (nestedSubDirs.Length == 1)
            {
                var nestedSubDir = nestedSubDirs[0];
                var nestedSubDirName = Path.GetFileName(nestedSubDir);

                if (nestedSubDirName.StartsWith("LenovoLegionToolkit.Plugins."))
                {
                    if (Lib.Utils.Log.Instance.IsTraceEnabled)
                        Lib.Utils.Log.Instance.Trace($"  Detected double-level nesting, flattening...");

                    var pluginId = nestedSubDirName.Replace("LenovoLegionToolkit.Plugins.", "");

                    // Move all contents from deeply nested directory to extractDir
                    await MoveDirectoryContentsAsync(nestedSubDir, extractDir);

                    // Delete the now-empty nested directories
                    Directory.Delete(nestedSubDir, true);
                    Directory.Delete(firstSubDir, true);

                    if (Lib.Utils.Log.Instance.IsTraceEnabled)
                        Lib.Utils.Log.Instance.Trace($"  Successfully flattened to plugin: {pluginId}");

                    return pluginId;
                }
            }

            // Case 3: Use the subdirectory name as plugin ID
            if (Lib.Utils.Log.Instance.IsTraceEnabled)
                Lib.Utils.Log.Instance.Trace($"  Using subdirectory as plugin ID: {firstSubDirName}");

            return firstSubDirName;
        }
        catch (Exception ex)
        {
            if (Lib.Utils.Log.Instance.IsTraceEnabled)
                Lib.Utils.Log.Instance.Trace($"Error analyzing plugin structure: {ex.Message}", ex);
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

private string GetPluginLocalizedDescription(IPlugin plugin)
    {
        var resourceName = $"Plugin_Description_{plugin.Id}";
        var property = typeof(Resource).GetProperty(resourceName);
        if (property != null)
        {
            var value = property.GetValue(null) as string;
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }
        return plugin.Description;
    }

    

    private void PluginListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_pluginsListBox.SelectedItem is PluginViewModel selectedViewModel)
        {
            var isInstalled = _pluginManager.IsInstalled(selectedViewModel.PluginId);
            if (isInstalled)
            {
                // For installed plugins, open configuration on double click
                OpenPluginConfiguration(selectedViewModel.PluginId);
            }
            else
            {
                // For uninstalled plugins, install them
                PluginInstallButton_Click(sender, e);
            }
        }
    }

    private void OpenPluginConfiguration(string pluginId)
    {
        if (Lib.Utils.Log.Instance.IsTraceEnabled)
            Lib.Utils.Log.Instance.Trace($"OpenPluginConfiguration called for {pluginId}");

        try
        {
            // Check if plugin is installed
            if (!_pluginManager.IsInstalled(pluginId))
            {
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    Lib.Utils.Log.Instance.Trace($"Plugin {pluginId} is not installed, configuration not available");
                
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_PluginNotInstalled, Resource.PluginExtensionsPage_PluginNotInstalledMessage);
                }
                return;
            }

            // Get plugin instance
            var plugin = _pluginManager.GetRegisteredPlugins().FirstOrDefault(p => p.Id == pluginId);
            if (plugin == null)
            {
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    Lib.Utils.Log.Instance.Trace($"Plugin {pluginId} is installed but not loaded");
                
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_ConfigurationFailed, Resource.PluginExtensionsPage_ConfigurationFailedMessage);
                }
                return;
            }

            if (Lib.Utils.Log.Instance.IsTraceEnabled)
                Lib.Utils.Log.Instance.Trace($"Plugin {pluginId} is loaded, opening configuration...");

            // For installed plugins, open configuration on double click
            if (plugin is LenovoLegionToolkit.Plugins.SDK.PluginBase sdkPlugin)
            {
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    Lib.Utils.Log.Instance.Trace($"Plugin {pluginId} is SDK plugin, checking GetSettingsPage...");

                var settingsPage = sdkPlugin.GetSettingsPage();
                if (settingsPage == null)
                {
                    if (Lib.Utils.Log.Instance.IsTraceEnabled)
                        Lib.Utils.Log.Instance.Trace($"Plugin {pluginId} does not provide a settings page");
                    
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_NoConfiguration, string.Format(Resource.PluginExtensionsPage_NoConfigurationMessage, plugin.Name));
                    }
                    return;
                }

                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    Lib.Utils.Log.Instance.Trace($"Plugin {pluginId} provides settings page: {settingsPage.GetType().Name}");

                // Open plugin settings window
                var window = new Windows.Settings.PluginSettingsWindow(pluginId)
                {
                    Owner = Window.GetWindow(this)
                };

                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    Lib.Utils.Log.Instance.Trace($"Opening PluginSettingsWindow for {pluginId}");
                
                window.ShowDialog();

                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    Lib.Utils.Log.Instance.Trace($"PluginSettingsWindow closed for {pluginId}");
            }
            else
            {
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    Lib.Utils.Log.Instance.Trace($"Plugin {pluginId} is not an SDK plugin (type: {plugin.GetType().Name}), configuration not supported");
                
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_ConfigurationNotSupported, string.Format(Resource.PluginExtensionsPage_ConfigurationNotSupportedMessage, plugin.Name));
                }
            }
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error opening plugin configuration for {pluginId}: {ex.Message}", ex);
            
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_OpenFailed, string.Format(Resource.PluginExtensionsPage_OpenFailedMessage, ex.Message));
            }
        }
        finally
        {
            if (Lib.Utils.Log.Instance.IsTraceEnabled)
                Lib.Utils.Log.Instance.Trace($"OpenPluginConfiguration completed for {pluginId}");
        }
    }

    private void PluginManager_PluginStateChanged(object? sender, PluginEventArgs e)
    {
        // Update UI when plugin state changes (installed/uninstalled)
        Dispatcher.Invoke(() =>
        {
            UpdateSpecificPluginUI(e.PluginId);
            UpdateAllPluginsUI();
        });
    }

    private void PluginExtensionsPage_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+Shift+R to reset all plugin states (for debugging/testing)
        if (e.Key == Key.R && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            e.Handled = true;
            ResetAllPluginStates();
        }
    }

    /// <summary>
    /// Reset all plugin installation states (for debugging/testing purposes)
    /// This clears the installed extensions list so plugins show as uninstalled
    /// </summary>
    private void ResetAllPluginStates()
    {
        try
        {
            if (Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                Lib.Utils.Log.Instance.Trace($"ResetAllPluginStates called");
            }

            // Clear all installed extensions
            var applicationSettings = IoCContainer.Resolve<LenovoLegionToolkit.Lib.Settings.ApplicationSettings>();
            applicationSettings.Store.InstalledExtensions.Clear();
            applicationSettings.SynchronizeStore();

            // Update UI for all plugins
            UpdateAllPluginsUI();

            // Show confirmation
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_PluginStatesReset, Resource.PluginExtensionsPage_PluginStatesResetMessage);
            }

            if (Lib.Utils.Log.Instance.IsTraceEnabled)
            {
                Lib.Utils.Log.Instance.Trace($"All plugin states have been reset");
            }
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error resetting plugin states: {ex.Message}", ex);
        }
    }
}
