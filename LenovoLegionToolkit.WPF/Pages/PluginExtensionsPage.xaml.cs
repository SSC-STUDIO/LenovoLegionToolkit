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
        // 工具箱和系统优化现在是默认应用，不再显示在插件市场中
        // 未来真正的插件系统将在这里显示第三方插件
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
        // 工具箱和系统优化现在是默认应用，不再显示在插件市场中
        // 未来真正的插件系统将在这里显示第三方插件的详细信息
        if (PluginDetailsPanel != null)
        {
            PluginDetailsPanel.Visibility = Visibility.Collapsed;
        }
    }
}
