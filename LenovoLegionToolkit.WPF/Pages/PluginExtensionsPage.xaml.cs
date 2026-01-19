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
    private string _currentSelectedPluginId = string.Empty;
    private bool _isRefreshing = false;
    private string _currentDownloadingPluginId = string.Empty;

    public PluginExtensionsPage()
    {
        _pluginRepositoryService = new PluginRepositoryService(_pluginManager);
        
        InitializeComponent();
        Loaded += PluginExtensionsPage_Loaded;
        IsVisibleChanged += PluginExtensionsPage_IsVisibleChanged;
        
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
        try
        {
            // Show loading indicator
            var noPluginsMessage = this.FindName("_noPluginsMessage") as StackPanel;
            if (noPluginsMessage != null)
            {
                noPluginsMessage.Visibility = Visibility.Collapsed;
            }
            
            // Fetch online plugins
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
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error fetching online plugins: {ex.Message}", ex);
            
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Snackbar.Show("获取插件失败", $"无法从插件商店获取插件列表: {ex.Message}");
            }
        }
    }
    
    private void ApplyFilters()
    {
        var filteredPlugins = _allPlugins.AsEnumerable();
        
        // Apply filter
        filteredPlugins = _currentFilter switch
        {
            "Online" => filteredPlugins.Where(p => _onlinePlugins.Any(op => op.Id == p.Id)),
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
        if (_pluginsItemsControl == null) return;
        
        _pluginsItemsControl.Items.Clear();
        
        // 去重：按插件 ID 去重
        var uniquePlugins = plugins.GroupBy(p => p.Id).Select(g => g.First()).ToList();
        
        foreach (var plugin in uniquePlugins)
        {
            try
            {
                var pluginCard = CreatePluginCard(plugin);
                _pluginsItemsControl.Items.Add(pluginCard);
            }
            catch (Exception ex)
            {
                Lib.Utils.Log.Instance.Trace($"Failed to create card for plugin {plugin.Id}: {ex.Message}", ex);
            }
        }
        
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

    private async void LoadPluginsFromRootDirectory()
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
            // 合并在线插件和本地注册的插件
            var allPluginsList = new List<IPlugin>();
            
            // 添加在线插件（使用适配器）
            if (_onlinePlugins != null && _onlinePlugins.Count > 0)
            {
                foreach (var onlinePlugin in _onlinePlugins)
                {
                    allPluginsList.Add(new PluginManifestAdapter(onlinePlugin));
                }
            }
            
            _allPlugins = allPluginsList;
            
            // 应用当前筛选和搜索
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
            
            // 确保"没有插件"消息在出错时也能显示
            if (_noPluginsMessage != null)
            {
                _noPluginsMessage.Visibility = Visibility.Visible;
            }
        }
    }

    /// <summary>
    /// Create plugin card UI element
    /// </summary>
    private Border CreatePluginCard(IPlugin plugin)
    {
        var border = new Border
        {
            Style = (Style)FindResource("ToolCardButtonStyle"),
            Tag = plugin.Id,
            Margin = new Thickness(0, 0, 12, 12),
            Opacity = 0,
            RenderTransform = new TranslateTransform(0, 20),
            Width = 200,
            Height = 200
        };

        border.MouseLeftButtonDown += PluginCard_MouseLeftButtonDown;
        
        border.Loaded += (s, e) =>
        {
            var fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            var slideAnimation = new DoubleAnimation
            {
                From = 20,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            border.BeginAnimation(Border.OpacityProperty, fadeInAnimation);
            border.RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
        };

        var grid = new Grid
        {
            Margin = new Thickness(0)
        };

        // 创建图标容器，填充整个卡片
        var iconBorder = new Border
        {
            CornerRadius = new CornerRadius(12),
            BorderBrush = (Brush)FindResource("ControlStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(1)
        };
        
        // 获取图标内容
        var iconContent = CreatePluginIconOrLetter(plugin);
        
        // 设置图标填充整个卡片
        if (iconContent is Image image)
        {
            image.Stretch = Stretch.UniformToFill;
            image.HorizontalAlignment = HorizontalAlignment.Stretch;
            image.VerticalAlignment = VerticalAlignment.Stretch;
        }
        
        // 使用 Grid 来叠加图标、徽章和名称
        var iconGrid = new Grid();
        iconGrid.Children.Add(iconContent);
        
        // 徽章容器（右上角）
        var badgeContainer = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 4, 4, 0)
        };
        
        // 已安装标签
        var isInstalled = _pluginManager.IsInstalled(plugin.Id);
        if (isInstalled)
        {
            var installedBadge = new Border
            {
                Background = (Brush)FindResource("SystemFillColorSuccessBrush"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(4, 2, 4, 2),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var badgeText = new TextBlock
            {
                Text = Resource.PluginExtensionsPage_PluginInstalled,
                FontSize = 8,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White
            };
            installedBadge.Child = badgeText;
            badgeContainer.Children.Add(installedBadge);
        }
        
        // 如果有徽章，则添加到网格中
        if (badgeContainer.Children.Count > 0)
        {
            iconGrid.Children.Add(badgeContainer);
        }
        
        // 创建底部名称覆盖层
        var nameOverlay = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
            CornerRadius = new CornerRadius(0, 0, 12, 12),
            Padding = new Thickness(12, 8, 12, 8),
            VerticalAlignment = VerticalAlignment.Bottom
        };
        
        var namePanel = new StackPanel
        {
            Orientation = Orientation.Vertical
        };
        
        // 插件名称（使用多语言资源）
        var displayName = GetPluginLocalizedName(plugin);
        var nameTextBlock = new TextBlock
        {
            Text = displayName,
            FontSize = 14,
            FontWeight = FontWeights.Medium,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.White
        };
        namePanel.Children.Add(nameTextBlock);
        
        // 插件版本（如果有）
        var metadata = _pluginManager.GetPluginMetadata(plugin.Id);
        if (metadata != null && !string.IsNullOrWhiteSpace(metadata.Version))
        {
            var versionTextBlock = new TextBlock
            {
                Text = $"v{metadata.Version}",
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255))
            };
            namePanel.Children.Add(versionTextBlock);
        }
        
        nameOverlay.Child = namePanel;
        iconGrid.Children.Add(nameOverlay);
        
        iconBorder.Child = iconGrid;
        grid.Children.Add(iconBorder);

        border.Child = grid;
        return border;
    }

    /// <summary>
    /// Create plugin icon (real image or colored letters)
    /// </summary>
    private UIElement CreatePluginIconOrLetter(IPlugin plugin)
    {
        var isInstalled = _pluginManager.IsInstalled(plugin.Id);
        
        if (isInstalled)
        {
            var icon = LoadPluginIcon(plugin);
            if (icon != null)
                return icon;
        }

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

        var suffixes = new[] { "插件", "Plugin", "plugin", "PLUG-IN", "Plug-in" };
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
                mainWindow?.Snackbar.Show("更新失败", "无法找到插件的在线版本");
                return;
            }

            var updateButton = this.FindName("PluginUpdateButton") as Wpf.Ui.Controls.Button;
            if (updateButton != null)
            {
                updateButton.IsEnabled = false;
                updateButton.Content = "更新中...";
            }

            mainWindow?.Snackbar.Show("正在更新插件", $"正在下载并更新 {onlinePlugin.Name}...");

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
                progressText.Text = "准备下载...";
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
                ShowPluginDetails(pluginId);

                mainWindow?.Snackbar.Show("更新成功", $"插件 {onlinePlugin.Name} 已成功更新到 v{onlinePlugin.Version}");
            }
            else
            {
                mainWindow?.Snackbar.Show("更新失败", "插件更新失败，请检查网络连接或稍后重试。");
            }
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error updating plugin: {ex.Message}", ex);

            mainWindow?.Snackbar.Show("更新失败", $"更新插件时出错：{ex.Message}");
        }
        finally
        {
            _currentDownloadingPluginId = string.Empty;

            var updateButton = this.FindName("PluginUpdateButton") as Wpf.Ui.Controls.Button;
            if (updateButton != null)
            {
                updateButton.IsEnabled = true;
                updateButton.Content = "更新";
            }

            var downloadProgressPanel = this.FindName("DownloadProgressPanel") as StackPanel;
            if (downloadProgressPanel != null)
            {
                downloadProgressPanel.Visibility = Visibility.Collapsed;
            }
        }
    }

    /// <summary>
    /// 将字符串转换为 SymbolRegular 枚举值
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
        // 工具箱和系统优化现在是默认应用，不再需要在这里更新
        // 未来真正的插件系统将在这里处理第三方插件
    }

    // 工具箱和系统优化现在是默认应用，相关代码已移除
    // 未来真正的插件系统将在这里实现第三方插件的安装和管理

    private void PluginCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;
        if (source != null)
        {
            var current = source;
            while (current != null)
            {
                if (current is Wpf.Ui.Controls.Button || current is System.Windows.Controls.Button)
                    return;
                current = VisualTreeHelper.GetParent(current);
            }
        }

        if (sender is not Border border)
            return;

        var pluginId = border.Tag?.ToString();
        if (string.IsNullOrWhiteSpace(pluginId))
            return;

        if (e.ClickCount == 2)
        {
            var isInstalled = _pluginManager.IsInstalled(pluginId);
            if (isInstalled)
            {
                PluginOpenButton_Click(sender, e);
            }
            else
            {
                PluginInstallButton_Click(sender, e);
            }
        }
        else
        {
            ShowPluginDetails(pluginId);
        }
        e.Handled = true;
    }

    private void CloseDetailsPanel_Click(object sender, RoutedEventArgs e)
    {
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
    }

    private void ShowPluginDetails(string pluginId)
    {
        try
        {
            var plugin = _pluginManager.GetRegisteredPlugins().FirstOrDefault(p => p.Id == pluginId);
            
            if (plugin == null)
            {
                plugin = _allPlugins.FirstOrDefault(p => p.Id == pluginId);
            }
            
            if (plugin == null)
                return;

            var detailsPanel = this.FindName("PluginDetailsPanel") as Border;
            if (detailsPanel == null)
                return;

            var pluginMetadata = _pluginManager.GetPluginMetadata(pluginId);
            
            var nameBlock = this.FindName("PluginDetailsName") as TextBlock;
            if (nameBlock != null)
            {
                var displayName = GetPluginLocalizedName(plugin);
                var nameText = displayName;
                if (pluginMetadata != null && !string.IsNullOrWhiteSpace(pluginMetadata.Version))
                {
                    nameText += $" v{pluginMetadata.Version}";
                }
                nameBlock.Text = nameText;
            }

            var descBlock = this.FindName("PluginDetailsDescription") as TextBlock;
            if (descBlock != null)
            {
                var descText = GetPluginLocalizedDescription(plugin);
                if (pluginMetadata != null)
                {
                    if (!string.IsNullOrWhiteSpace(pluginMetadata.Author))
                    {
                        descText += $"\n{string.Format(Resource.PluginSettingsWindow_Author, pluginMetadata.Author)}";
                    }
                    if (!string.IsNullOrWhiteSpace(pluginMetadata.MinimumHostVersion))
                    {
                        descText += $"\n{string.Format(Resource.PluginExtensionsPage_MinimumVersion, pluginMetadata.MinimumHostVersion)}";
                    }
                }
                descBlock.Text = descText;
            }

            var isInstalled = _pluginManager.IsInstalled(pluginId);
            var installButton = this.FindName("PluginInstallButton") as Wpf.Ui.Controls.Button;
            var uninstallButton = this.FindName("PluginUninstallButton") as Wpf.Ui.Controls.Button;
            var openButton = this.FindName("PluginOpenButton") as Wpf.Ui.Controls.Button;

            if (installButton != null)
            {
                installButton.Visibility = isInstalled ? Visibility.Collapsed : Visibility.Visible;
                installButton.Tag = pluginId;
            }

            if (uninstallButton != null)
            {
                uninstallButton.Visibility = isInstalled ? Visibility.Visible : Visibility.Collapsed;
                uninstallButton.Tag = pluginId;
            }

            if (openButton != null)
            {
                openButton.Visibility = isInstalled ? Visibility.Visible : Visibility.Collapsed;
                openButton.Tag = pluginId;
            }
            
            var configureButton = this.FindName("PluginConfigureButton") as Wpf.Ui.Controls.Button;
            if (configureButton != null)
            {
                var supportsConfig = false;
                if (plugin is Plugins.SDK.PluginBase sdkPlugin)
                {
                    var featureExtension = sdkPlugin.GetFeatureExtension();
                    supportsConfig = featureExtension is Plugins.SDK.IPluginPage;
                }
                configureButton.Visibility = (isInstalled && supportsConfig) ? Visibility.Visible : Visibility.Collapsed;
                configureButton.Tag = pluginId;
            }

            var updateButton = this.FindName("PluginUpdateButton") as Wpf.Ui.Controls.Button;
            if (updateButton != null)
            {
                var hasUpdate = CheckPluginHasUpdate(pluginId);
                updateButton.Visibility = (isInstalled && hasUpdate) ? Visibility.Visible : Visibility.Collapsed;
                updateButton.Tag = pluginId;
            }

            _currentSelectedPluginId = pluginId;
            SetupLanguageSelection(pluginId);

            var showStoryboard = this.FindResource("ShowDetailsPanel") as Storyboard;
            if (showStoryboard != null)
            {
                detailsPanel.Visibility = Visibility.Visible;
                showStoryboard.Begin();
            }
            else
            {
                detailsPanel.Visibility = Visibility.Visible;
                detailsPanel.Opacity = 1;
            }
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error showing plugin details: {ex.Message}", ex);
        }
    }

    private void PluginInstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.Button button || button.Tag is not string pluginId)
            return;

        // Check if this is an online plugin installation
        var onlinePlugin = _onlinePlugins.FirstOrDefault(p => p.Id == pluginId);
        if (onlinePlugin != null)
        {
            InstallOnlinePluginAsync(onlinePlugin);
            return;
        }

        try
        {
            _pluginManager.InstallPlugin(pluginId);
            
            // 刷新 UI
            ShowPluginDetails(pluginId);
            UpdateAllPluginsUI();
            
            // 显示成功消息
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_InstallSuccess, Resource.PluginExtensionsPage_InstallSuccessMessage);
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

    private async void InstallOnlinePluginAsync(PluginManifest manifest)
    {
        try
        {
            _currentDownloadingPluginId = manifest.Id;
            
            var mainWindow = Application.Current.MainWindow as MainWindow;
            
            // Show installing message
            if (mainWindow != null)
            {
                mainWindow.Snackbar.Show("正在安装插件", $"正在下载并安装 {manifest.Name}...");
            }
            
            // Show download progress panel
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
                progressText.Text = "准备下载...";
            }
            
            // Disable install button during installation
            var installButton = this.FindName("PluginInstallButton") as Wpf.Ui.Controls.Button;
            if (installButton != null)
            {
                installButton.IsEnabled = false;
                installButton.Content = "安装中...";
            }
            
            // Subscribe to download progress
            _pluginRepositoryService.DownloadProgressChanged += OnDownloadProgressChanged;
            
            // Download and install plugin
            var success = await _pluginRepositoryService.DownloadAndInstallPluginAsync(manifest);
            
            // Unsubscribe from download progress
            _pluginRepositoryService.DownloadProgressChanged -= OnDownloadProgressChanged;
            
            if (success)
            {
                // Refresh UI
                _pluginManager.ScanAndLoadPlugins();
                LocalizationHelper.SetPluginResourceCultures();
                UpdateAllPluginsUI();
                ShowPluginDetails(manifest.Id);
                
                if (mainWindow != null)
                {
                    mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_InstallSuccess, $"插件 {manifest.Name} 已成功安装！");
                }
            }
            else
            {
                if (mainWindow != null)
                {
                    mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_InstallFailed, $"插件安装失败，请检查网络连接或稍后重试。");
                }
            }
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error installing online plugin {manifest.Id}: {ex.Message}", ex);
            
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_InstallFailed, $"安装插件时出错: {ex.Message}");
            }
        }
        finally
        {
            // Reset install button state
            var installButton = this.FindName("PluginInstallButton") as Wpf.Ui.Controls.Button;
            if (installButton != null)
            {
                installButton.IsEnabled = true;
                installButton.Content = Resource.ResourceManager.GetString("PluginExtensionsPage_InstallPlugin", Resource.Culture) ?? "Install";
            }
            
            // Hide download progress panel
            var downloadProgressPanel = this.FindName("DownloadProgressPanel") as StackPanel;
            if (downloadProgressPanel != null)
            {
                downloadProgressPanel.Visibility = Visibility.Collapsed;
            }
            
            _currentDownloadingPluginId = string.Empty;
        }
    }

    private void OnDownloadProgressChanged(object? sender, PluginDownloadProgress progress)
    {
        if (!string.IsNullOrEmpty(_currentDownloadingPluginId) && progress.PluginId != _currentDownloadingPluginId)
            return;
            
        if (progress.PluginId == _currentDownloadingPluginId)
        {
            var progressBar = this.FindName("DownloadProgressBar") as System.Windows.Controls.ProgressBar;
            var progressText = this.FindName("DownloadProgressText") as TextBlock;
            
            if (progressBar != null)
            {
                progressBar.Value = progress.ProgressPercentage;
            }
            
            if (progressText != null)
            {
                if (progress.IsCompleted)
                {
                    progressText.Text = "下载完成";
                }
                else if (progress.TotalBytes > 0)
                {
                    var downloadedMB = progress.BytesDownloaded / 1024.0 / 1024.0;
                    var totalMB = progress.TotalBytes / 1024.0 / 1024.0;
                    progressText.Text = $"下载中... {downloadedMB:F1} / {totalMB:F1} MB ({progress.ProgressPercentage:F0}%)";
                }
                else
                {
                    progressText.Text = "下载中...";
                }
            }
        }
    }
    

    private void PluginUninstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.Button button || button.Tag is not string pluginId)
            return;

        try
        {
            _pluginManager.PermanentlyDeletePlugin(pluginId);
            
            var detailsPanel = this.FindName("PluginDetailsPanel") as Border;
            if (detailsPanel != null)
            {
                detailsPanel.Visibility = Visibility.Collapsed;
            }
            
            UpdateAllPluginsUI();
            
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

    private void PluginConfigureButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.Button button || button.Tag is not string pluginId)
            return;

        try
        {
            var window = new Windows.Settings.PluginSettingsWindow(pluginId)
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error opening plugin settings: {ex.Message}", ex);
            
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_OpenFailed, string.Format(Resource.PluginExtensionsPage_OpenFailedMessage, ex.Message));
            }
        }
    }

    private void PluginOpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.Button button || button.Tag is not string pluginId)
            return;

        try
        {
            // 确保插件已安装
            if (!_pluginManager.IsInstalled(pluginId))
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.Snackbar.Show(Resource.PluginExtensionsPage_PluginNotInstalled, Resource.PluginExtensionsPage_PluginNotInstalledMessage);
                }
                return;
            }

            // 查找插件的可执行文件
            var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", pluginId);
            var exeFile = Path.Combine(pluginDir, $"{pluginId}.exe");
            
            if (File.Exists(exeFile))
            {
                // 运行插件的可执行文件
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
                    mainWindow.Snackbar.Show("运行插件", $"已启动 {pluginId}.exe");
                }
            }
            else
            {
                // 导航到插件页面（如果可执行文件不存在）
                var mainWindow2 = Application.Current.MainWindow as MainWindow;
                if (mainWindow2 != null)
                {
                    // 使用 NavigationStore 导航到插件页面
                    var navigationStore = mainWindow2.FindName("_navigationStore") as NavigationStore;
                    if (navigationStore != null)
                    {
                        // 注册页面标签到插件ID的映射
                        PluginPageWrapper.RegisterPluginPageTag($"plugin:{pluginId}", pluginId);
                        
                        // 创建一个临时导航项来导航到插件页面
                        var tempItem = new NavigationItem
                    {
                        PageTag = $"plugin:{pluginId}",
                        PageType = typeof(PluginPageWrapper)
                    };
                    
                        // 导航到插件页面
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

        // 显示确认对话框
        var result = await MessageBoxHelper.ShowAsync(this, 
            "永久删除插件", 
            $"确定要永久删除插件 \"{plugin.Name}\" 吗？\n\n此操作无法撤销，插件文件将被永久删除。",
            "删除",
            "取消");

        if (!result)
            return;

        // 执行永久删除
        try
        {
            _pluginManager.PermanentlyDeletePlugin(pluginId);
            
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
                mainWindow.Snackbar.Show("插件已删除", "插件文件已永久删除，无法恢复。");
            }
        }
        catch (Exception ex)
        {
            Lib.Utils.Log.Instance.Trace($"Error permanently deleting plugin: {ex.Message}", ex);
            
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.Snackbar.Show("删除失败", $"删除插件时发生错误: {ex.Message}");
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
                            // Continue searching
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (Lib.Utils.Log.Instance.IsTraceEnabled)
                Lib.Utils.Log.Instance.Trace($"Error applying language to plugin {pluginId}: {ex.Message}", ex);
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
}
