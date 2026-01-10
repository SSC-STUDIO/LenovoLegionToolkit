using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Windows;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

// Use IPluginPage from Plugins.SDK
using IPluginPage = LenovoLegionToolkit.Plugins.SDK.IPluginPage;

namespace LenovoLegionToolkit.WPF.Pages;

/// <summary>
/// 插件页面包装器，用于承载插件提供的UI页面
/// </summary>
public partial class PluginPageWrapper : UiPage
{
    private static readonly Dictionary<string, string> PageTagToPluginIdMap = new();
    
    private readonly IPluginManager _pluginManager = IoCContainer.Resolve<IPluginManager>();
    private string? _pluginId;

    public PluginPageWrapper()
    {
        InitializeComponent();
        Loaded += PluginPageWrapper_Loaded;
    }

    /// <summary>
    /// 注册 PageTag 到插件ID的映射
    /// </summary>
    public static void RegisterPluginPageTag(string pageTag, string pluginId)
    {
        PageTagToPluginIdMap[pageTag] = pluginId;
    }

    private void PluginPageWrapper_Loaded(object sender, RoutedEventArgs e)
    {
        // 从导航上下文中获取插件ID
        // NavigationStore 使用 PageTag 来标识页面，格式为 "plugin:{pluginId}"
        if (_pluginId == null)
        {
            // 尝试从父窗口的导航存储中获取当前页面的 PageTag
            var mainWindow = Application.Current.MainWindow as LenovoLegionToolkit.WPF.Windows.MainWindow;
            if (mainWindow != null)
            {
                var navigationStore = mainWindow.FindName("_navigationStore") as NavigationStore;
                if (navigationStore?.Current != null)
                {
                    var pageTag = navigationStore.Current.PageTag;
                    if (pageTag != null)
                    {
                        // 首先尝试从映射字典中获取
                        if (PageTagToPluginIdMap.TryGetValue(pageTag, out var mappedPluginId))
                        {
                            _pluginId = mappedPluginId;
                        }
                        // 如果 PageTag 格式为 "plugin:{pluginId}"，直接解析
                        else if (pageTag.StartsWith("plugin:"))
                        {
                            _pluginId = pageTag.Substring("plugin:".Length);
                        }
                    }
                }
            }
        }

        if (_pluginId != null)
        {
            LoadPluginPage();
        }
    }

    private void LoadPluginPage()
    {
        try
        {
            var plugin = _pluginManager.GetRegisteredPlugins().FirstOrDefault(p => p.Id == _pluginId);
            if (plugin == null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin {_pluginId} not found");
                return;
            }

            IPluginPage? pluginPage = null;

            // 检查插件是否支持 GetFeatureExtension 方法（SDK 插件）
            var pluginType = plugin.GetType();
            var getFeatureExtensionMethod = pluginType.GetMethod("GetFeatureExtension", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (getFeatureExtensionMethod != null)
            {
                var featureExtension = getFeatureExtensionMethod.Invoke(plugin, null);
                if (featureExtension is IPluginPage page)
                {
                    pluginPage = page;
                }
            }
            
            // System Optimization and Tools are now default interfaces, not plugins
            // They are accessed directly via NavigationItems in MainWindow.xaml
            // If plugin does not provide IPluginPage, log and return
            if (pluginPage == null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin {_pluginId} does not provide IPluginPage");
                return;
            }

            // 设置页面标题
            Title = pluginPage.PageTitle;
            
            // 设置页面图标和标题显示
            var pluginHeader = this.FindName("_pluginHeader") as StackPanel;
            if (pluginHeader != null)
            {
                // Only show header if PageTitle is not empty (plugins can hide it by returning empty string)
                if (!string.IsNullOrWhiteSpace(pluginPage.PageTitle))
                {
                    pluginHeader.Visibility = Visibility.Visible;
                    
                    // 设置图标
                    var pluginIcon = this.FindName("_pluginIcon") as Wpf.Ui.Controls.SymbolIcon;
                    if (pluginIcon != null && !string.IsNullOrWhiteSpace(pluginPage.PageIcon))
                    {
                        if (Enum.TryParse<SymbolRegular>(pluginPage.PageIcon, out var icon))
                        {
                            pluginIcon.Symbol = icon;
                            pluginIcon.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Failed to parse icon '{pluginPage.PageIcon}' for plugin {_pluginId}");
                            pluginIcon.Visibility = Visibility.Collapsed;
                        }
                    }
                    else if (pluginIcon != null)
                    {
                        pluginIcon.Visibility = Visibility.Collapsed;
                    }
                    
                    // 设置标题
                    var pluginTitle = this.FindName("_pluginTitle") as TextBlock;
                    if (pluginTitle != null)
                    {
                        pluginTitle.Text = pluginPage.PageTitle;
                    }
                }
                else
                {
                    // Hide header if PageTitle is empty
                    pluginHeader.Visibility = Visibility.Collapsed;
                }
            }
            
            // 创建插件页面控件
            var pluginControl = pluginPage.CreatePage();
            
            // Find the Frame control by name
            var frame = this.FindName("_pluginContentFrame") as Frame;
            if (frame == null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"PluginPageWrapper: _pluginContentFrame not found");
                return;
            }
            
            if (pluginControl is System.Windows.Controls.Page pageControl)
            {
                // 如果是 Page，使用 Frame 导航
                frame.Navigate(pageControl);
            }
            else if (pluginControl is UIElement uiElement)
            {
                // 如果是其他 UIElement，使用 ContentPresenter（通过 Frame 的内容区域）
                frame.Content = uiElement;
            }
            else
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin {_pluginId} CreatePage() did not return a UIElement or Page");
            }
        }
        catch (System.Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to load plugin page for {_pluginId}", ex);
        }
    }
}
