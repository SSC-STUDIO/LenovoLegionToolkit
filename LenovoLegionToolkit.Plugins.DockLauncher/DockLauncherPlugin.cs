using System;
using System.Linq;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.SDK;
using System.Windows.Media.Animation;
using PluginConstants = LenovoLegionToolkit.Lib.Plugins.PluginConstants;

namespace LenovoLegionToolkit.Plugins.DockLauncher;

/// <summary>
/// Dock launcher plugin for macOS-style application launcher
/// </summary>
[Plugin(
    id: PluginConstants.DockLauncher,
    name: "Dock Launcher",
    version: "1.0.0",
    description: "macOS-style dock launcher for quick application access and window management",
    author: "LenovoLegionToolkit Team",
    MinimumHostVersion = "1.0.0",
    Icon = "Apps24"
)]
public class DockLauncherPlugin : PluginBase
{
    private DockWindow? _dockWindow;
    private static bool _startupCheckDone = false;

    static DockLauncherPlugin()
    {
        // Check and show dock on plugin class load (app startup)
        CheckAndShowDockOnStartup();
    }

    private static void CheckAndShowDockOnStartup()
    {
        if (_startupCheckDone)
            return;
            
        _startupCheckDone = true;
        
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(3000); // Wait for app to fully initialize
                var settings = new Services.Settings.DockSettings();
                await settings.LoadAsync();
                
                if (settings.IsEnabled)
                {
                    if (System.Windows.Application.Current != null)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                // Get plugin instance from plugin manager
                                var pluginManager = Lib.IoCContainer.Resolve<Lib.Plugins.IPluginManager>();
                                var plugin = pluginManager.GetRegisteredPlugins()
                                    .FirstOrDefault(p => p.Id == PluginConstants.DockLauncher) as DockLauncherPlugin;
                                
                                if (plugin != null)
                                {
                                    plugin.ShowDockWindow();
                                    plugin.RefreshDockWindow();
                                    
                                    if (Lib.Utils.Log.Instance.IsTraceEnabled)
                                        Lib.Utils.Log.Instance.Trace($"DockLauncherPlugin: Auto-showing dock window on app startup (IsEnabled=True)");
                                }
                            }
                            catch (Exception ex)
                            {
                                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                                    Lib.Utils.Log.Instance.Trace($"DockLauncherPlugin: Error in CheckAndShowDockOnStartup: {ex.Message}", ex);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    Lib.Utils.Log.Instance.Trace($"DockLauncherPlugin: Error checking dock on startup: {ex.Message}", ex);
            }
        });
    }

    public override string Id => PluginConstants.DockLauncher;
    public override string Name => Resource.DockLauncher_PageTitle;
    public override string Description => Resource.DockLauncher_PageDescription;
    public override string Icon => "Apps24";
    public override bool IsSystemPlugin => false;

    public override object? GetFeatureExtension()
    {
        return new DockLauncherPluginPage();
    }

    public override object? GetSettingsPage()
    {
        return new DockLauncherSettingsPluginPage();
    }

    public override void OnInstalled()
    {
        base.OnInstalled();
        // Show dock window on startup if enabled
        // This is called when plugin is installed, but we also want it to run on app startup
        ShowDockIfEnabled();
    }

    private void ShowDockIfEnabled()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(3000); // Wait for app to fully initialize
                var settings = new Services.Settings.DockSettings();
                await settings.LoadAsync();
                
                if (settings.IsEnabled)
                {
                    if (System.Windows.Application.Current != null)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            ShowDockWindow();
                            RefreshDockWindow();
                        });
                        
                        if (Lib.Utils.Log.Instance.IsTraceEnabled)
                            Lib.Utils.Log.Instance.Trace($"DockLauncherPlugin: Auto-showing dock window on startup (IsEnabled=True)");
                    }
                }
            }
            catch (Exception ex)
            {
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    Lib.Utils.Log.Instance.Trace($"DockLauncherPlugin: Error auto-showing dock on startup: {ex.Message}", ex);
            }
        });
    }

    public override void OnUninstalled()
    {
        base.OnUninstalled();
        CloseDockWindow();
    }

    internal void ShowDockWindow()
    {
        try
        {
            if (_dockWindow == null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"DockLauncherPlugin: Creating new dock window");
                _dockWindow = new DockWindow();
            }
            
            // Stop any running animations first
            var hideAnimation = _dockWindow.Resources["HideAnimation"] as System.Windows.Media.Animation.Storyboard;
            var showAnimation = _dockWindow.Resources["ShowAnimation"] as System.Windows.Media.Animation.Storyboard;
            hideAnimation?.Stop(_dockWindow);
            showAnimation?.Stop(_dockWindow);
            
            // Ensure window is on top and visible before showing
            _dockWindow.Topmost = true;
            _dockWindow.Visibility = System.Windows.Visibility.Visible;
            _dockWindow.Opacity = 1.0; // Force opacity to 1.0
            _dockWindow.ShowInTaskbar = true; // Show in taskbar
            _dockWindow.WindowState = System.Windows.WindowState.Normal;
            
            if (!_dockWindow.IsVisible)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"DockLauncherPlugin: Showing dock window (IsVisible=false)");
                _dockWindow.Show();
            }
            
            // Activate and bring to front
            _dockWindow.Activate();
            _dockWindow.BringIntoView();
            
            // Force opacity again after showing to ensure it's visible
            _dockWindow.Opacity = 1.0;
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPlugin: Dock window shown - IsVisible={_dockWindow.IsVisible}, Opacity={_dockWindow.Opacity}, Left={_dockWindow.Left}, Top={_dockWindow.Top}");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPlugin: Error showing dock window: {ex.Message}", ex);
        }
    }

    internal void RefreshDockWindow()
    {
        if (_dockWindow != null)
        {
            _dockWindow.RefreshDockItems();
        }
    }

    internal void ForceShowDockWindow()
    {
        ShowDockWindow();
    }

    internal void CloseDockWindow()
    {
        if (_dockWindow != null)
        {
            _dockWindow.Close();
            _dockWindow = null;
        }
    }
}

/// <summary>
/// Dock launcher plugin page provider
/// </summary>
public class DockLauncherPluginPage : IPluginPage
{
    public string PageTitle => string.Empty;
    public string PageIcon => string.Empty;

    public object CreatePage()
    {
        return new DockLauncherPage();
    }
}

/// <summary>
/// Dock launcher settings plugin page provider
/// </summary>
public class DockLauncherSettingsPluginPage : IPluginPage
{
    public string PageTitle => string.Empty;
    public string PageIcon => string.Empty;

    public object CreatePage()
    {
        return new DockLauncherSettingsPage();
    }
}


                            RefreshDockWindow();
                        });
                        
                        if (Lib.Utils.Log.Instance.IsTraceEnabled)
                            Lib.Utils.Log.Instance.Trace($"DockLauncherPlugin: Auto-showing dock window on startup (IsEnabled=True)");
                    }
                }
            }
            catch (Exception ex)
            {
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    Lib.Utils.Log.Instance.Trace($"DockLauncherPlugin: Error auto-showing dock on startup: {ex.Message}", ex);
            }
        });
    }

    public override void OnUninstalled()
    {
        base.OnUninstalled();
        CloseDockWindow();
    }

    internal void ShowDockWindow()
    {
        try
        {
            if (_dockWindow == null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"DockLauncherPlugin: Creating new dock window");
                _dockWindow = new DockWindow();
            }
            
            // Stop any running animations first
            var hideAnimation = _dockWindow.Resources["HideAnimation"] as System.Windows.Media.Animation.Storyboard;
            var showAnimation = _dockWindow.Resources["ShowAnimation"] as System.Windows.Media.Animation.Storyboard;
            hideAnimation?.Stop(_dockWindow);
            showAnimation?.Stop(_dockWindow);
            
            // Ensure window is on top and visible before showing
            _dockWindow.Topmost = true;
            _dockWindow.Visibility = System.Windows.Visibility.Visible;
            _dockWindow.Opacity = 1.0; // Force opacity to 1.0
            _dockWindow.ShowInTaskbar = true; // Show in taskbar
            _dockWindow.WindowState = System.Windows.WindowState.Normal;
            
            if (!_dockWindow.IsVisible)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"DockLauncherPlugin: Showing dock window (IsVisible=false)");
                _dockWindow.Show();
            }
            
            // Activate and bring to front
            _dockWindow.Activate();
            _dockWindow.BringIntoView();
            
            // Force opacity again after showing to ensure it's visible
            _dockWindow.Opacity = 1.0;
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPlugin: Dock window shown - IsVisible={_dockWindow.IsVisible}, Opacity={_dockWindow.Opacity}, Left={_dockWindow.Left}, Top={_dockWindow.Top}");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPlugin: Error showing dock window: {ex.Message}", ex);
        }
    }

    internal void RefreshDockWindow()
    {
        if (_dockWindow != null)
        {
            _dockWindow.RefreshDockItems();
        }
    }

    internal void ForceShowDockWindow()
    {
        ShowDockWindow();
    }

    internal void CloseDockWindow()
    {
        if (_dockWindow != null)
        {
            _dockWindow.Close();
            _dockWindow = null;
        }
    }
}

/// <summary>
/// Dock launcher plugin page provider
/// </summary>
public class DockLauncherPluginPage : IPluginPage
{
    public string PageTitle => string.Empty;
    public string PageIcon => string.Empty;

    public object CreatePage()
    {
        return new DockLauncherPage();
    }
}

/// <summary>
/// Dock launcher settings plugin page provider
/// </summary>
public class DockLauncherSettingsPluginPage : IPluginPage
{
    public string PageTitle => string.Empty;
    public string PageIcon => string.Empty;

    public object CreatePage()
    {
        return new DockLauncherSettingsPage();
    }
}


using LenovoLegionToolkit.Plugins.SDK;
using PluginConstants = LenovoLegionToolkit.Lib.Plugins.PluginConstants;

namespace LenovoLegionToolkit.Plugins.DockLauncher;

/// <summary>
/// Dock launcher plugin for macOS-style application launcher
/// </summary>
[Plugin(
    id: PluginConstants.DockLauncher,
    name: "Dock Launcher",
    version: "1.0.0",
    description: "macOS-style dock launcher for quick application access and window management",
    author: "LenovoLegionToolkit Team",
    MinimumHostVersion = "1.0.0",
    Icon = "Apps24"
)]
public class DockLauncherPlugin : PluginBase
{
    private DockWindow? _dockWindow;

    public override string Id => PluginConstants.DockLauncher;
    public override string Name => Resource.DockLauncher_PageTitle;
    public override string Description => Resource.DockLauncher_PageDescription;
    public override string Icon => "Apps24";
    public override bool IsSystemPlugin => false;

    public override object? GetFeatureExtension()
    {
        return new DockLauncherPluginPage();
    }

    public override object? GetSettingsPage()
    {
        return new DockLauncherSettingsPluginPage();
    }

    public override void OnInstalled()
    {
        base.OnInstalled();
        // Dock window will be shown/hidden based on settings
    }

    public override void OnUninstalled()
    {
        base.OnUninstalled();
        CloseDockWindow();
    }

    internal void ShowDockWindow()
    {
        try
        {
            if (_dockWindow == null)
            {
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    Lib.Utils.Log.Instance.Trace($"DockLauncherPlugin: Creating new dock window");
                _dockWindow = new DockWindow();
            }
            
            if (!_dockWindow.IsVisible)
            {
                if (Lib.Utils.Log.Instance.IsTraceEnabled)
                    Lib.Utils.Log.Instance.Trace($"DockLauncherPlugin: Showing dock window");
                _dockWindow.Show();
                _dockWindow.Activate();
            }
            
            // Ensure window is on top and visible
            _dockWindow.Topmost = true;
            _dockWindow.Visibility = System.Windows.Visibility.Visible;
            _dockWindow.Opacity = 1.0;
        }
        catch (Exception ex)
        {
            if (Lib.Utils.Log.Instance.IsTraceEnabled)
                Lib.Utils.Log.Instance.Trace($"DockLauncherPlugin: Error showing dock window: {ex.Message}", ex);
        }
    }

    internal void RefreshDockWindow()
    {
        if (_dockWindow != null)
        {
            _dockWindow.RefreshDockItems();
        }
    }

    internal void CloseDockWindow()
    {
        if (_dockWindow != null)
        {
            _dockWindow.Close();
            _dockWindow = null;
        }
    }
}

/// <summary>
/// Dock launcher plugin page provider
/// </summary>
public class DockLauncherPluginPage : IPluginPage
{
    public string PageTitle => string.Empty;
    public string PageIcon => string.Empty;

    public object CreatePage()
    {
        return new DockLauncherPage();
    }
}

/// <summary>
/// Dock launcher settings plugin page provider
/// </summary>
public class DockLauncherSettingsPluginPage : IPluginPage
{
    public string PageTitle => string.Empty;
    public string PageIcon => string.Empty;

    public object CreatePage()
    {
        return new DockLauncherSettingsPage();
    }
}

