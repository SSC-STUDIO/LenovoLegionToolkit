using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Controls.Custom;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Windows;
using UniversalDeviceToolkit.WPF.Windows.Settings;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Pages
{
/// <summary>
/// 插件页面包装器，用于承载插件提供的UI页面
/// </summary>
public partial class PluginPageWrapper : Page
{
    private static readonly ConcurrentDictionary<string, string> PageTagToPluginIdMap = new();

    private readonly IPluginManager _pluginManager = IoCContainer.Resolve<IPluginManager>();
    private string? _pluginId;
    private bool _loadingPluginPage;

    public PluginPageWrapper()
    {
        InitializeComponent();
        Loaded += PluginPageWrapper_Loaded;
        Unloaded += PluginPageWrapper_Unloaded;
    }

    public PluginPageWrapper(string pluginId) : this()
    {
        _pluginId = pluginId;
    }

    /// <summary>
    /// 注册 PageTag 到插件ID的映�?    /// </summary>
    public static void RegisterPluginPageTag(string pageTag, string pluginId)
    {
        PageTagToPluginIdMap[pageTag] = pluginId;
    }

    private void PluginPageWrapper_Loaded(object sender, RoutedEventArgs e)
    {
        // 从导航上下文中获取插件ID
        // NavigationStore 使用 PageTag 来标识页面，格式�?"plugin:{pluginId}"
        if (_pluginId == null)
        {
            // 尝试从父窗口的导航存储中获取当前页面�?PageTag
            var mainWindow = Application.Current.MainWindow as UniversalDeviceToolkit.WPF.Windows.MainWindow;
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
                        // 如果 PageTag 格式�?"plugin:{pluginId}"，直接解�?                        else if (pageTag.StartsWith("plugin:"))
                        {
                            _pluginId = pageTag.Substring("plugin:".Length);
                        }
                    }
                }
            }
        }

        if (_pluginId == null)
        {
            ShowEmptyState(T("PluginPageWrapper_UnableToResolve", "Unable to resolve plugin entry. Please return to Plugin Extensions and reopen this plugin."));
            return;
        }

        LoadPluginPage();
    }

    private void PluginPageWrapper_Unloaded(object sender, RoutedEventArgs e)
    {
    }

    private void LoadPluginPage()
    {
        if (_loadingPluginPage)
            return;

        try
        {
            _loadingPluginPage = true;
            FlowDirection = LocalizationHelper.Direction;

            var plugin = _pluginManager.GetRegisteredPlugins().FirstOrDefault(p => p.Id == _pluginId);
            if (plugin == null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin {_pluginId} not found");
                ShowEmptyState(string.Format(
                    T("PluginPageWrapper_PluginUnavailable", "Plugin '{0}' is not available."),
                    _pluginId));
                return;
            }

            var metadata = _pluginManager.GetPluginMetadata(_pluginId!);
            if (HasIncompatibleWpfUiDependency(metadata))
            {
                ShowCompatibilityFallback(plugin, metadata);
                return;
            }

            var pluginPage = ResolvePluginPage(plugin);

            // System Optimization and Tools are now default interfaces, not plugins
            // They are accessed directly via NavigationItems in MainWindow.xaml
            // If plugin does not provide IPluginPage, log and return
            if (pluginPage == null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin {_pluginId} does not provide IPluginPage");
                ShowEmptyState(string.Format(
                    T("PluginPageWrapper_NoFeaturePage", "Plugin '{0}' does not provide a feature page."),
                    plugin.Name));
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
                        if (Enum.TryParse<SymbolRegular>(pluginPage.PageIcon, ignoreCase: true, out var icon))
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
            var contentHost = this.FindName("_pluginContentHost") as ContentControl;
            if (contentHost == null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"PluginPageWrapper: _pluginContentHost not found");
                ShowEmptyState(T("PluginPageWrapper_ContentUnavailable", "Plugin content container is unavailable."));
                return;
            }

            if (pluginControl is UIElement uiElement)
            {
                contentHost.Content = uiElement;
                HideEmptyState();
            }
            else
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin {_pluginId} CreatePage() did not return a UIElement or Page");
                ShowEmptyState(string.Format(
                    T("PluginPageWrapper_InvalidPage", "Plugin '{0}' did not return a valid UI page."),
                    plugin.Name));
            }
        }
        catch (System.Exception ex)
        {
            var displayException = UnwrapInvocationException(ex);
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to load plugin page for {_pluginId}: {displayException.Message}", ex);
            ShowEmptyState(string.Format(
                T("PluginPageWrapper_LoadFailed", "Failed to load plugin page: {0}"),
                displayException.Message));
        }
        finally
        {
            _loadingPluginPage = false;
        }
    }

    internal static bool ProvidesFeaturePage(IPlugin plugin) => ResolvePluginPage(plugin) != null;

    internal static bool TryCreateHostedPluginPage(object? pageSource, out HostedPluginPage hostedPage)
    {
        hostedPage = default!;

        if (pageSource == null)
            return false;

        if (pageSource is IPluginPage pluginPage)
        {
            hostedPage = new HostedPluginPage(
                pluginPage.PageTitle,
                pluginPage.PageIcon,
                pluginPage.CreatePage);
            return true;
        }

        var reflectionPage = TryCreateReflectionPluginPage(pageSource);
        if (reflectionPage == null)
            return false;

        hostedPage = reflectionPage;
        return true;
    }

    private static HostedPluginPage? ResolvePluginPage(IPlugin plugin)
    {
        var pluginType = plugin.GetType();
        var getFeatureExtensionMethod = pluginType.GetMethod("GetFeatureExtension", BindingFlags.Public | BindingFlags.Instance);
        if (getFeatureExtensionMethod == null)
            return null;

        var featureExtension = InvokePluginMethod(getFeatureExtensionMethod, plugin);
        if (featureExtension == null)
            return null;

        return TryCreateHostedPluginPage(featureExtension, out var pluginPage) ? pluginPage : null;
    }

    private static HostedPluginPage? TryCreateReflectionPluginPage(object featureExtension)
    {
        var pageType = featureExtension.GetType();
        var createPageMethod = pageType.GetMethod("CreatePage", BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes);
        if (createPageMethod == null)
            return null;

        var title = ReadStringProperty(pageType, featureExtension, "PageTitle") ?? string.Empty;
        var icon = ReadStringProperty(pageType, featureExtension, "PageIcon");

        return new HostedPluginPage(
            title,
            icon,
            () => InvokePluginMethod(createPageMethod, featureExtension) ?? new object());
    }

    private static string? ReadStringProperty(Type type, object instance, string propertyName)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(instance) as string;
    }

    private static object? InvokePluginMethod(MethodInfo method, object instance)
    {
        try
        {
            return method.Invoke(instance, null);
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw new InvalidOperationException(ex.InnerException.Message, ex.InnerException);
        }
    }

    private static Exception UnwrapInvocationException(Exception exception)
    {
        return exception is TargetInvocationException { InnerException: not null } targetInvocationException
            ? targetInvocationException.InnerException!
            : exception.InnerException ?? exception;
    }

    private void ShowEmptyState(string message)
    {
        var emptyStateBorder = this.FindName("_emptyStateBorder") as Border;
        var emptyStateText = this.FindName("_emptyStateTextBlock") as TextBlock;
        var contentHost = this.FindName("_pluginContentHost") as ContentControl;

        if (contentHost != null)
            contentHost.Content = null;

        if (emptyStateText != null)
            emptyStateText.Text = message;

        if (emptyStateBorder != null)
            emptyStateBorder.Visibility = Visibility.Visible;
    }

    private void ShowCompatibilityFallback(IPlugin plugin, PluginMetadata? metadata)
    {
        var pluginName = string.IsNullOrWhiteSpace(plugin.Name) ? _pluginId ?? plugin.Id : plugin.Name;
        var pluginVersion = metadata?.WpfUiVersion ?? "unknown";
        var hostVersion = typeof(SymbolIcon).Assembly.GetName().Version?.ToString() ?? "unknown";
        ShowEmptyState(string.Format(
            T(
                "PluginPageWrapper_IncompatibleWpfUi",
                "Plugin '{0}' targets Wpf.Ui {1}, but the host is running Wpf.Ui {2}. The feature page is hidden to keep the app stable."),
            pluginName,
            pluginVersion,
            hostVersion));
    }

    private static bool HasIncompatibleWpfUiDependency(PluginMetadata? metadata)
    {
        if (metadata == null || string.IsNullOrWhiteSpace(metadata.WpfUiVersion))
            return false;

        if (!Version.TryParse(metadata.WpfUiVersion, out var pluginWpfUiVersion))
            return false;

        var hostWpfUiVersion = typeof(SymbolIcon).Assembly.GetName().Version;
        if (hostWpfUiVersion == null)
            return false;

        return pluginWpfUiVersion.Major != hostWpfUiVersion.Major;
    }

    private void HideEmptyState()
    {
        var emptyStateBorder = this.FindName("_emptyStateBorder") as Border;
        if (emptyStateBorder != null)
            emptyStateBorder.Visibility = Visibility.Collapsed;

        var emptyStateText = this.FindName("_emptyStateTextBlock") as TextBlock;
        if (emptyStateText != null)
            emptyStateText.Text = string.Empty;
    }

    private static string T(string key, string fallback)
    {
        return LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);
    }

    internal sealed record HostedPluginPage(string PageTitle, string? PageIcon, Func<object> CreatePage);

}
}
