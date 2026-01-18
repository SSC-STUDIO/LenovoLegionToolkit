using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        
        // Apply filter - 只有外置插件，没有内置和第三方之分
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
        if (_pluginsItemsControl == null) return;
        
        _pluginsItemsControl.Items.Clear();
        
        foreach (var plugin in plugins)
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
            _resultsCountTextBlock.Text = string.Format(Resource.PluginExtensionsPage_FoundPluginsCount, plugins.Count);
            _resultsCountTextBlock.Visibility = plugins.Any() ? Visibility.Visible : Visibility.Collapsed;
        }
        
        // Show/hide no plugins message
        if (_noPluginsMessage != null)
        {
            _noPluginsMessage.Visibility = plugins.Any() ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void PluginExtensionsPage_Loaded(object sender, RoutedEventArgs e)
    {
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

    private void PluginExtensionsPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            // Use Dispatcher to ensure UI updates happen after plugin scanning
            Dispatcher.BeginInvoke(new Action(() =>
            {
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
            
            // 添加本地注册的插件（避免重复）
            var localPlugins = _pluginManager.GetRegisteredPlugins().ToList();
            foreach (var localPlugin in localPlugins)
            {
                if (!allPluginsList.Any(p => p.Id == localPlugin.Id))
                {
                    allPluginsList.Add(localPlugin);
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
    /// 创建插件卡片 UI 元素
    /// </summary>
    private Border CreatePluginCard(IPlugin plugin)
    {
        var border = new Border
        {
            Style = (Style)FindResource("ToolCardButtonStyle"),
            Tag = plugin.Id,
            Margin = new Thickness(0, 0, 12, 12),
            Opacity = 0,
            RenderTransform = new TranslateTransform(0, 20)
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

        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // 插件图标容器
        var iconContainer = new Grid
        {
            Width = 48,
            Height = 48,
            Margin = new Thickness(0, 0, 0, 8),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        var iconBorder = new Border
        {
            Background = (Brush)FindResource("ControlFillColorSecondaryBrush"),
            CornerRadius = new CornerRadius(8),
            BorderBrush = (Brush)FindResource("ControlStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(1),
            Width = 48,
            Height = 48
        };
        
        // 生成彩色字母图标
        var iconContent = CreateColoredLetterIcon(plugin);
        iconBorder.Child = iconContent;
        iconContainer.Children.Add(iconBorder);
        
        // 已安装标签
        if (_pluginManager.IsInstalled(plugin.Id))
        {
            var installedBadge = new Border
            {
                Background = (Brush)FindResource("SystemFillColorSuccessBrush"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(4, 2, 4, 2),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -4, -4, 0)
            };
            var badgeText = new TextBlock
            {
                Text = Resource.PluginExtensionsPage_PluginInstalled,
                FontSize = 8,
                FontWeight = FontWeights.Medium,
                Foreground = Brushes.White
            };
            installedBadge.Child = badgeText;
            iconContainer.Children.Add(installedBadge);
        }
        
        stackPanel.Children.Add(iconContainer);

        // 插件名称（移除"插件"字样）
        var displayName = RemovePluginSuffix(plugin.Name);
        var nameTextBlock = new TextBlock
        {
            Text = displayName,
            FontSize = 12,
            FontWeight = FontWeights.Medium,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 100
        };
        nameTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        stackPanel.Children.Add(nameTextBlock);
        
        // 插件版本（如果有）
        var metadata = _pluginManager.GetPluginMetadata(plugin.Id);
        if (metadata != null && !string.IsNullOrWhiteSpace(metadata.Version))
        {
            var versionTextBlock = new TextBlock
            {
                Text = $"v{metadata.Version}",
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };
            versionTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            stackPanel.Children.Add(versionTextBlock);
        }

        border.Child = stackPanel;
        return border;
    }

    /// <summary>
    /// 创建彩色字母图标
    /// </summary>
    private UIElement CreateColoredLetterIcon(IPlugin plugin)
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

        var colors = new List<SolidColorBrush>
        {
            new SolidColorBrush(Color.FromRgb(59, 130, 246)),
            new SolidColorBrush(Color.FromRgb(239, 68, 68)),
            new SolidColorBrush(Color.FromRgb(245, 158, 11)),
            new SolidColorBrush(Color.FromRgb(34, 197, 94)),
            new SolidColorBrush(Color.FromRgb(168, 85, 247)),
            new SolidColorBrush(Color.FromRgb(13, 148, 136)),
            new SolidColorBrush(Color.FromRgb(233, 30, 99)),
            new SolidColorBrush(Color.FromRgb(42, 161, 152))
        };

        var grid = new Grid
        {
            Width = 48,
            Height = 48
        };

        if (letters.Count >= 2)
        {
            var leftPanel = new Grid
            {
                Width = 24,
                Height = 48,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var rightPanel = new Grid
            {
                Width = 24,
                Height = 48,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var leftLetter = new TextBlock
            {
                Text = letters[0].ToString().ToUpper(),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = colors[0]
            };

            var rightLetter = new TextBlock
            {
                Text = letters[1].ToString().ToUpper(),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = colors[1]
            };

            leftPanel.Children.Add(leftLetter);
            rightPanel.Children.Add(rightLetter);
            grid.Children.Add(leftPanel);
            grid.Children.Add(rightPanel);
        }
        else if (letters.Count == 1)
        {
            var letter = new TextBlock
            {
                Text = letters[0].ToString().ToUpper(),
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = colors[0]
            };
            grid.Children.Add(letter);
        }
        else
        {
            var icon = new Wpf.Ui.Controls.SymbolIcon
            {
                Symbol = SymbolRegular.Apps24,
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            icon.SetResourceReference(Control.ForegroundProperty, "SystemAccentColorBrush");
            grid.Children.Add(icon);
        }

        return grid;
    }

    /// <summary>
    /// 移除插件名称中的"插件"后缀
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
            
            var icon = this.FindName("PluginDetailsIcon") as Wpf.Ui.Controls.SymbolIcon;
            if (icon != null)
            {
                icon.Symbol = GetSymbolFromString(plugin.Icon);
                icon.Foreground = (System.Windows.Media.Brush)FindResource("SystemAccentColorBrush");
            }

            var nameBlock = this.FindName("PluginDetailsName") as TextBlock;
            if (nameBlock != null)
            {
                var nameText = plugin.Name;
                if (pluginMetadata != null && !string.IsNullOrWhiteSpace(pluginMetadata.Version))
                {
                    nameText += $" v{pluginMetadata.Version}";
                }
                nameBlock.Text = nameText;
            }

            var descBlock = this.FindName("PluginDetailsDescription") as TextBlock;
            if (descBlock != null)
            {
                var descText = plugin.Description;
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

            var permanentlyDeleteButton = this.FindName("PluginPermanentlyDeleteButton") as Wpf.Ui.Controls.Button;
            if (permanentlyDeleteButton != null)
            {
                permanentlyDeleteButton.Visibility = !plugin.IsSystemPlugin ? Visibility.Visible : Visibility.Collapsed;
                permanentlyDeleteButton.Tag = pluginId;
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
            _pluginManager.UninstallPlugin(pluginId);
            
            // 刷新 UI
            ShowPluginDetails(pluginId);
            UpdateAllPluginsUI();
            
            // 显示成功消息
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
            var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "build", "plugins", pluginId);
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
        
        // Apply the language change immediately
        if (cultureInfo != null)
        {
            ApplyPluginLanguage(pluginId, cultureInfo);
        }
        else
        {
            // Use application default
            ApplyPluginLanguage(pluginId, Resource.Culture ?? CultureInfo.CurrentUICulture);
        }
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
            var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "build", "plugins", plugin.Id);
            
            var iconExtensions = new[] { ".png", ".jpg", ".jpeg", ".ico", ".svg" };
            string? iconPath = null;
            
            foreach (var ext in iconExtensions)
            {
                var testPath = Path.Combine(pluginDir, $"icon{ext}");
                if (File.Exists(testPath))
                {
                    iconPath = testPath;
                    break;
                }
            }
            
            if (!string.IsNullOrEmpty(iconPath))
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
            
            var icon = new Wpf.Ui.Controls.SymbolIcon
            {
                Symbol = GetSymbolFromString(plugin.Icon),
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            icon.SetResourceReference(Control.ForegroundProperty, "SystemAccentColorBrush");
            return icon;
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
}
