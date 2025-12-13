using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Windows;
using Wpf.Ui.Common;

namespace LenovoLegionToolkit.WPF.Pages;

public partial class PluginExtensionsPage
{
    private readonly ApplicationSettings _applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly IPluginManager _pluginManager = IoCContainer.Resolve<IPluginManager>();

    public PluginExtensionsPage()
    {
        InitializeComponent();
        Loaded += PluginExtensionsPage_Loaded;
        IsVisibleChanged += PluginExtensionsPage_IsVisibleChanged;
        
        // 设置页面标题和文本（使用动态资源获取，避免自动生成资源的问题）
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

    private void PluginExtensionsPage_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateAllPluginsUI();
    }

    private void PluginExtensionsPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            UpdateAllPluginsUI();
        }
    }

    private void UpdateAllPluginsUI()
    {
        foreach (var plugin in _pluginManager.GetRegisteredPlugins())
        {
            UpdatePluginUI(plugin.Id);
        }
    }

    private void UpdatePluginUI(string pluginId)
    {
        var hasPlugin = _pluginManager.IsInstalled(pluginId);
        var hasOtherPlugins = _pluginManager.GetInstalledPluginIds()
            .Any(ext => !string.Equals(ext, pluginId, StringComparison.OrdinalIgnoreCase));

        if (pluginId == PluginConstants.SystemOptimization)
        {
            UpdateSystemOptimizationPluginUI(hasPlugin, hasOtherPlugins);
        }
        else if (pluginId == PluginConstants.Tools)
        {
            UpdateToolsPluginUI(hasPlugin);
        }
        else if (pluginId == PluginConstants.Cleanup)
        {
            UpdateCleanupPluginUI(hasPlugin);
        }
        else if (pluginId == PluginConstants.DriverDownload)
        {
            UpdateDriverDownloadPluginUI(hasPlugin);
        }
    }

    private void UpdateSystemOptimizationPluginUI(bool hasPlugin, bool hasOtherPlugins)
    {
        var pluginName = this.FindName("_systemOptimizationPluginName") as TextBlock;
        var pluginDescription = this.FindName("_systemOptimizationPluginDescription") as TextBlock;
        var pluginActions = this.FindName("_systemOptimizationPluginActions") as StackPanel;
        var installedStatus = this.FindName("_systemOptimizationInstalledStatus") as StackPanel;
        var installButtonText = this.FindName("_systemOptimizationInstallButtonText") as TextBlock;
        var installedStatusText = this.FindName("_systemOptimizationInstalledStatusText") as TextBlock;
        var uninstallButton = this.FindName("_systemOptimizationUninstallButton") as Wpf.Ui.Controls.Button;
        var uninstallButtonInStatus = this.FindName("_systemOptimizationUninstallButtonInStatus") as Wpf.Ui.Controls.Button;

        if (pluginName != null)
        {
            pluginName.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_SystemOptimization_Title", Resource.Culture) ?? "系统优化插件";
        }

        if (pluginDescription != null)
        {
            pluginDescription.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_SystemOptimization_Description", Resource.Culture) ?? 
                "安装系统优化插件以访问系统优化和清理功能";
        }

        if (pluginActions != null && installedStatus != null)
        {
            if (hasPlugin || hasOtherPlugins)
            {
                pluginActions.Visibility = Visibility.Collapsed;
                installedStatus.Visibility = Visibility.Visible;

                if (installedStatusText != null)
                {
                    installedStatusText.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_PluginInstalled", Resource.Culture) ?? "已安装";
                }

                var uninstallButtons = new List<Wpf.Ui.Controls.Button>();
                if (uninstallButton != null) uninstallButtons.Add(uninstallButton);
                if (uninstallButtonInStatus != null) uninstallButtons.Add(uninstallButtonInStatus);

                foreach (var btn in uninstallButtons)
                {
                    if (hasOtherPlugins)
                    {
                        btn.IsEnabled = false;
                        btn.ToolTip = Resource.ResourceManager.GetString("PluginExtensionsPage_CannotUninstallBasePlugin", Resource.Culture) ??
                            "系统优化插件是基础插件，当有其他插件安装时无法卸载";
                    }
                    else
                    {
                        btn.IsEnabled = true;
                        btn.ToolTip = null;
                    }
                }
            }
            else
            {
                pluginActions.Visibility = Visibility.Visible;
                installedStatus.Visibility = Visibility.Collapsed;

                if (installButtonText != null)
                {
                    installButtonText.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_InstallPlugin", Resource.Culture) ?? "安装";
                }
            }
        }
    }

    private void UpdateToolsPluginUI(bool hasPlugin)
    {
        var pluginName = this.FindName("_toolsPluginName") as TextBlock;
        var pluginDescription = this.FindName("_toolsPluginDescription") as TextBlock;
        var pluginActions = this.FindName("_toolsPluginActions") as StackPanel;
        var installedStatus = this.FindName("_toolsInstalledStatus") as StackPanel;
        var installButtonText = this.FindName("_toolsInstallButtonText") as TextBlock;
        var installedStatusText = this.FindName("_toolsInstalledStatusText") as TextBlock;

        if (pluginName != null)
        {
            pluginName.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_Tools_Title", Resource.Culture) ?? "工具箱插件";
        }

        if (pluginDescription != null)
        {
            pluginDescription.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_Tools_Description", Resource.Culture) ??
                "安装工具箱插件以访问各种系统工具和实用程序";
        }

        if (pluginActions != null && installedStatus != null)
        {
            if (hasPlugin)
            {
                pluginActions.Visibility = Visibility.Collapsed;
                installedStatus.Visibility = Visibility.Visible;

                if (installedStatusText != null)
                {
                    installedStatusText.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_PluginInstalled", Resource.Culture) ?? "已安装";
                }
            }
            else
            {
                pluginActions.Visibility = Visibility.Visible;
                installedStatus.Visibility = Visibility.Collapsed;

                if (installButtonText != null)
                {
                    installButtonText.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_InstallPlugin", Resource.Culture) ?? "安装";
                }
            }
        }
    }

    private void InstallPlugin(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return;

        _pluginManager.InstallPlugin(pluginId);

        // 更新UI
        UpdateAllPluginsUI();

        // 更新主窗口导航栏
        if (Application.Current.MainWindow is MainWindow mainWindow)
        {
            mainWindow.UpdateNavigationVisibility();
        }
    }

    private void _systemOptimizationPluginButton_Click(object sender, RoutedEventArgs e)
    {
        InstallPlugin(PluginConstants.SystemOptimization);
    }

    private void _systemOptimizationUninstallButton_Click(object sender, RoutedEventArgs e)
    {
        UninstallSystemOptimizationPlugin();
    }

    private void UninstallSystemOptimizationPlugin()
    {
        if (!_pluginManager.UninstallPlugin(PluginConstants.SystemOptimization))
        {
            System.Windows.MessageBox.Show(
                Resource.ResourceManager.GetString("PluginExtensionsPage_CannotUninstallBasePluginMessage", Resource.Culture) ??
                "系统优化插件是基础插件。当有其他插件安装时，系统优化插件无法卸载。请先卸载其他插件后再卸载系统优化插件。",
                Resource.ResourceManager.GetString("PluginExtensionsPage_CannotUninstallTitle", Resource.Culture) ?? "无法卸载",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        UpdateAllPluginsUI();

        if (Application.Current.MainWindow is MainWindow mainWindow)
        {
            mainWindow.UpdateNavigationVisibility();
        }
    }

    private void _toolsPluginButton_Click(object sender, RoutedEventArgs e)
    {
        InstallPlugin(PluginConstants.Tools);
    }

    private void _toolsUninstallButton_Click(object sender, RoutedEventArgs e)
    {
        UninstallToolsPlugin();
    }

    private void UninstallToolsPlugin()
    {
        _pluginManager.UninstallPlugin(PluginConstants.Tools);

        UpdateAllPluginsUI();

        if (Application.Current.MainWindow is MainWindow mainWindow)
        {
            mainWindow.UpdateNavigationVisibility();
        }
    }

    private void UpdateCleanupPluginUI(bool hasPlugin)
    {
        var pluginName = this.FindName("_cleanupPluginName") as TextBlock;
        var pluginDescription = this.FindName("_cleanupPluginDescription") as TextBlock;
        var pluginActions = this.FindName("_cleanupPluginActions") as StackPanel;
        var installedStatus = this.FindName("_cleanupInstalledStatus") as StackPanel;
        var installButtonText = this.FindName("_cleanupInstallButtonText") as TextBlock;
        var installedStatusText = this.FindName("_cleanupInstalledStatusText") as TextBlock;

        if (pluginName != null)
        {
            pluginName.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_Cleanup_Title", Resource.Culture) ?? "垃圾清理插件";
        }

        if (pluginDescription != null)
        {
            pluginDescription.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_Cleanup_Description", Resource.Culture) ??
                "安装垃圾清理插件以访问系统清理功能";
        }

        if (pluginActions != null && installedStatus != null)
        {
            if (hasPlugin)
            {
                pluginActions.Visibility = Visibility.Collapsed;
                installedStatus.Visibility = Visibility.Visible;

                if (installedStatusText != null)
                {
                    installedStatusText.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_PluginInstalled", Resource.Culture) ?? "已安装";
                }
            }
            else
            {
                pluginActions.Visibility = Visibility.Visible;
                installedStatus.Visibility = Visibility.Collapsed;

                if (installButtonText != null)
                {
                    installButtonText.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_InstallPlugin", Resource.Culture) ?? "安装";
                }
            }
        }
    }

    private void UpdateDriverDownloadPluginUI(bool hasPlugin)
    {
        var pluginName = this.FindName("_driverDownloadPluginName") as TextBlock;
        var pluginDescription = this.FindName("_driverDownloadPluginDescription") as TextBlock;
        var pluginActions = this.FindName("_driverDownloadPluginActions") as StackPanel;
        var installedStatus = this.FindName("_driverDownloadInstalledStatus") as StackPanel;
        var installButtonText = this.FindName("_driverDownloadInstallButtonText") as TextBlock;
        var installedStatusText = this.FindName("_driverDownloadInstalledStatusText") as TextBlock;

        if (pluginName != null)
        {
            pluginName.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_DriverDownload_Title", Resource.Culture) ?? "驱动下载插件";
        }

        if (pluginDescription != null)
        {
            pluginDescription.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_DriverDownload_Description", Resource.Culture) ??
                "安装驱动下载插件以访问驱动下载功能";
        }

        if (pluginActions != null && installedStatus != null)
        {
            if (hasPlugin)
            {
                pluginActions.Visibility = Visibility.Collapsed;
                installedStatus.Visibility = Visibility.Visible;

                if (installedStatusText != null)
                {
                    installedStatusText.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_PluginInstalled", Resource.Culture) ?? "已安装";
                }
            }
            else
            {
                pluginActions.Visibility = Visibility.Visible;
                installedStatus.Visibility = Visibility.Collapsed;

                if (installButtonText != null)
                {
                    installButtonText.Text = Resource.ResourceManager.GetString("PluginExtensionsPage_InstallPlugin", Resource.Culture) ?? "安装";
                }
            }
        }
    }

    private void _cleanupPluginButton_Click(object sender, RoutedEventArgs e)
    {
        InstallPlugin(PluginConstants.Cleanup);
    }

    private void _cleanupUninstallButton_Click(object sender, RoutedEventArgs e)
    {
        UninstallCleanupPlugin();
    }

    private void UninstallCleanupPlugin()
    {
        _pluginManager.UninstallPlugin(PluginConstants.Cleanup);

        UpdateAllPluginsUI();
    }

    private void _driverDownloadPluginButton_Click(object sender, RoutedEventArgs e)
    {
        InstallPlugin(PluginConstants.DriverDownload);
    }

    private void _driverDownloadUninstallButton_Click(object sender, RoutedEventArgs e)
    {
        UninstallDriverDownloadPlugin();
    }

    private void UninstallDriverDownloadPlugin()
    {
        _pluginManager.UninstallPlugin(PluginConstants.DriverDownload);

        UpdateAllPluginsUI();
    }

    private void PluginCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;
        if (source != null)
        {
            var current = source;
            while (current != null)
            {
                if (current is Wpf.Ui.Controls.Button || current is Button)
                    return;
                current = VisualTreeHelper.GetParent(current);
            }
        }

        if (sender is not Border border)
            return;

        var pluginId = border.Tag?.ToString();
        if (string.IsNullOrWhiteSpace(pluginId))
            return;

        ShowPluginDetails(pluginId);
        e.Handled = true;
    }

    private void ShowPluginDetails(string pluginId)
    {
        if (PluginDetailsPanel == null)
            return;

        var metadata = _pluginManager.GetPluginMetadata(pluginId);
        if (metadata == null)
            return;

        string pluginName;
        string pluginDescription;
        SymbolRegular icon;

        if (pluginId == PluginConstants.SystemOptimization)
        {
            pluginName = Resource.ResourceManager.GetString("PluginExtensionsPage_SystemOptimization_Title", Resource.Culture) ?? metadata.Name;
            pluginDescription = Resource.ResourceManager.GetString("PluginExtensionsPage_SystemOptimization_Description", Resource.Culture) ?? metadata.Description;
            icon = SymbolRegular.Gauge24;
        }
        else if (pluginId == PluginConstants.Tools)
        {
            pluginName = Resource.ResourceManager.GetString("PluginExtensionsPage_Tools_Title", Resource.Culture) ?? metadata.Name;
            pluginDescription = Resource.ResourceManager.GetString("PluginExtensionsPage_Tools_Description", Resource.Culture) ?? metadata.Description;
            icon = SymbolRegular.Toolbox24;
        }
        else if (pluginId == PluginConstants.Cleanup)
        {
            pluginName = Resource.ResourceManager.GetString("PluginExtensionsPage_Cleanup_Title", Resource.Culture) ?? metadata.Name;
            pluginDescription = Resource.ResourceManager.GetString("PluginExtensionsPage_Cleanup_Description", Resource.Culture) ?? metadata.Description;
            icon = SymbolRegular.Delete24;
        }
        else if (pluginId == PluginConstants.DriverDownload)
        {
            pluginName = Resource.ResourceManager.GetString("PluginExtensionsPage_DriverDownload_Title", Resource.Culture) ?? metadata.Name;
            pluginDescription = Resource.ResourceManager.GetString("PluginExtensionsPage_DriverDownload_Description", Resource.Culture) ?? metadata.Description;
            icon = SymbolRegular.ArrowDownload24;
        }
        else
        {
            // 对于其他插件，使用元数据中的信息
            pluginName = metadata.Name;
            pluginDescription = metadata.Description;
            // 尝试从图标字符串解析 SymbolRegular
            if (Enum.TryParse<SymbolRegular>(metadata.Icon, out var parsedIcon))
            {
                icon = parsedIcon;
            }
            else
            {
                icon = SymbolRegular.Info24;
            }
        }

        PluginDetailsPanel.Visibility = Visibility.Visible;

        if (PluginDetailsIcon != null)
        {
            PluginDetailsIcon.Symbol = icon;
        }

        if (PluginDetailsName != null)
        {
            PluginDetailsName.Text = pluginName;
        }

        if (PluginDetailsDescription != null)
        {
            PluginDetailsDescription.Text = pluginDescription;
        }
    }
}
