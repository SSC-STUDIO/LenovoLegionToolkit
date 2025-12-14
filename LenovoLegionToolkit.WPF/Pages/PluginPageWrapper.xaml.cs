using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Resources;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Pages;

/// <summary>
/// 插件页面接口，用于插件提供UI页面
/// </summary>
internal interface IPluginPage
{
    /// <summary>
    /// 页面标题
    /// </summary>
    string PageTitle { get; }

    /// <summary>
    /// 页面图标（WPF UI Symbol）
    /// </summary>
    string? PageIcon { get; }

    /// <summary>
    /// 创建页面控件
    /// </summary>
    /// <returns>UI元素</returns>
    object CreatePage();
}

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
            var mainWindow = Application.Current.MainWindow as Windows.MainWindow;
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
            
            // 如果没有通过 GetFeatureExtension 获取到页面，尝试直接根据插件ID加载内置页面
            if (pluginPage == null)
            {
                if (_pluginId == PluginConstants.SystemOptimization)
                {
                    // 直接加载系统优化页面（使用 Frame 导航）
                    var windowsOptimizationPage = new WindowsOptimizationPage();
                    _pluginContentFrame.Navigate(windowsOptimizationPage);
                    
                    // 设置页面标题
                    Title = Resource.ResourceManager.GetString("MainWindow_NavigationItem_WindowsOptimization", Resource.Culture) ?? "System optimization";
                    
                    // 隐藏插件头部（因为系统优化页面有自己的头部）
                    if (_pluginHeader != null)
                    {
                        _pluginHeader.Visibility = Visibility.Collapsed;
                    }
                    
                    return;
                }
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin {_pluginId} does not provide IPluginPage and is not a built-in plugin");
                return;
            }

            // 设置页面标题
            Title = pluginPage.PageTitle;
            
            // 设置页面图标和标题显示
            if (_pluginHeader != null)
            {
                _pluginHeader.Visibility = Visibility.Visible;
                
                // 设置图标
                if (_pluginIcon != null && !string.IsNullOrWhiteSpace(pluginPage.PageIcon))
                {
                    if (Enum.TryParse<SymbolRegular>(pluginPage.PageIcon, out var icon))
                    {
                        _pluginIcon.Symbol = icon;
                        _pluginIcon.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Failed to parse icon '{pluginPage.PageIcon}' for plugin {_pluginId}");
                        _pluginIcon.Visibility = Visibility.Collapsed;
                    }
                }
                else if (_pluginIcon != null)
                {
                    _pluginIcon.Visibility = Visibility.Collapsed;
                }
                
                // 设置标题
                if (_pluginTitle != null)
                {
                    _pluginTitle.Text = pluginPage.PageTitle;
                }
            }
            
            // 创建插件页面控件
            var pluginControl = pluginPage.CreatePage();
            if (pluginControl is System.Windows.Controls.Page pageControl)
            {
                // 如果是 Page，使用 Frame 导航
                _pluginContentFrame.Navigate(pageControl);
            }
            else if (pluginControl is UIElement uiElement)
            {
                // 如果是其他 UIElement，使用 ContentPresenter（通过 Frame 的内容区域）
                _pluginContentFrame.Content = uiElement;
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
