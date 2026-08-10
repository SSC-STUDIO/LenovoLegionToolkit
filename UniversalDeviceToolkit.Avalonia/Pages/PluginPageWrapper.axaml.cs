using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Controls.Custom;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Windows;

namespace UniversalDeviceToolkit.Avalonia.Pages
{
/// <summary>
/// Plugin page wrapper for hosting plugin-provided UI pages
/// </summary>
public partial class PluginPageWrapper : global::Avalonia.Controls.UserControl
{
    private static readonly ConcurrentDictionary<string, string> PageTagToPluginIdMap = new();

    private readonly IPluginManager _pluginManager = IoCContainer.Resolve<IPluginManager>();
    private string? _pluginId;
    private bool _loadingPluginPage;

    public PluginPageWrapper()
    {
        InitializeComponent();
        Loaded += PluginPageWrapper_Loaded;
    }

    public PluginPageWrapper(string pluginId) : this()
    {
        _pluginId = pluginId;
    }

    /// <summary>
    /// Registers PageTag to plugin ID mapping
    /// </summary>
    public static void RegisterPluginPageTag(string pageTag, string pluginId)
    {
        PageTagToPluginIdMap[pageTag] = pluginId;
    }

    private void PluginPageWrapper_Loaded(object? sender, RoutedEventArgs e)
    {
        // Get plugin ID from navigation context
        // NavigationStore uses PageTag to identify pages, format: "plugin:{pluginId}"
        if (_pluginId == null)
        {
            // Try to get the current page from the parent window navigation store via PageTag
            var mainWindow = UdtAppContext.MainWindow as UniversalDeviceToolkit.Avalonia.Windows.MainWindow;
            if (mainWindow != null)
            {
                var navigationStore = (mainWindow as INameScope)?.Find("_navigationStore") as NavigationStore;
                if (navigationStore?.Current != null)
                {
                    var pageTag = navigationStore.Current.PageTag;
                    if (pageTag != null)
                    {
                        // First try to get from the mapping dictionary
                        if (PageTagToPluginIdMap.TryGetValue(pageTag, out var mappedPluginId))
                        {
                            _pluginId = mappedPluginId;
                        }
                        // If PageTag format is "plugin:{pluginId}", parse it directly
                        else if (pageTag.StartsWith("plugin:", StringComparison.OrdinalIgnoreCase))
                        {
                            _pluginId = pageTag["plugin:".Length..];
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

            // AVALONIA: removed Title = pluginPage.PageTitle (UserControl has no Title; header is rendered in-page).

            // Set page icon and title display
            // Only show header if PageTitle is not empty (plugins can hide it by returning empty string)
            if (!string.IsNullOrWhiteSpace(pluginPage.PageTitle))
            {
                _pluginHeader.IsVisible = true;

                // Set icon
                if (_pluginIcon != null && !string.IsNullOrWhiteSpace(pluginPage.PageIcon))
                {
                    if (Enum.TryParse<SymbolRegular>(pluginPage.PageIcon, ignoreCase: true, out var icon))
                    {
                        _pluginIcon.Symbol = icon;
                        _pluginIcon.IsVisible = true;
                    }
                    else
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Failed to parse icon '{pluginPage.PageIcon}' for plugin {_pluginId}");
                        _pluginIcon.IsVisible = false;
                    }
                }
                else if (_pluginIcon != null)
                {
                    _pluginIcon.IsVisible = false;
                }

                // Set title
                _pluginTitle.Text = pluginPage.PageTitle;
            }
            else
            {
                // Hide header if PageTitle is empty
                _pluginHeader.IsVisible = false;
            }

            // Create plugin page control
            var pluginControl = pluginPage.CreatePage();

            if (pluginControl is Control uiElement)
            {
                _pluginContentHost.Content = uiElement;
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
        _pluginContentHost.Content = null;
        _emptyStateControl.Description = message;
        _emptyStateBorder.IsVisible = true;
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
        _emptyStateBorder.IsVisible = false;
        _emptyStateControl.Description = string.Empty;
    }

    private static string T(string key, string fallback)
    {
        return LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);
    }

    internal sealed record HostedPluginPage(string PageTitle, string? PageIcon, Func<object> CreatePage);

}
}
