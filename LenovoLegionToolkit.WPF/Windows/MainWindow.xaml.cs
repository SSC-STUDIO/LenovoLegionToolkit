using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Listeners;
using LenovoLegionToolkit.Lib.Messaging;
using LenovoLegionToolkit.Lib.Messaging.Messages;
using LenovoLegionToolkit.Lib.Plugins;
using PluginConstants = LenovoLegionToolkit.Lib.Plugins.PluginConstants;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.SoftwareDisabler;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Extensions;
using LenovoLegionToolkit.WPF.Pages;
using NavigationItem = LenovoLegionToolkit.WPF.Controls.Custom.NavigationItem;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;
using LenovoLegionToolkit.WPF.Windows.Utils;
using Microsoft.Xaml.Behaviors.Core;
using Windows.Win32;
using Windows.Win32.System.Threading;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
#if !DEBUG
using System.Reflection;
using LenovoLegionToolkit.Lib.Extensions;
#endif

#pragma warning disable CA1416

namespace LenovoLegionToolkit.WPF.Windows;

public partial class MainWindow
{
    private readonly ApplicationSettings _applicationSettings;
    private readonly IPluginManager _pluginManager;
    private readonly SpecialKeyListener _specialKeyListener;
    private readonly VantageDisabler _vantageDisabler;
    private readonly LegionZoneDisabler _legionZoneDisabler;
    private readonly FnKeysDisabler _fnKeysDisabler;
    private readonly UpdateChecker _updateChecker;

    private TrayHelper? _trayHelper;
    private readonly Dictionary<string, NavigationItem> _pluginNavigationItems = new();

    public bool TrayTooltipEnabled { get; set; } = true;
    public bool DisableConflictingSoftwareWarning { get; set; }
    public bool SuppressClosingEventHandler { get; set; }

    public Snackbar Snackbar => _snackbar;

    public MainWindow(
        ApplicationSettings applicationSettings,
        IPluginManager pluginManager,
        SpecialKeyListener specialKeyListener,
        VantageDisabler vantageDisabler,
        LegionZoneDisabler legionZoneDisabler,
        FnKeysDisabler fnKeysDisabler,
        UpdateChecker updateChecker)
    {
        _applicationSettings = applicationSettings;
        _pluginManager = pluginManager;
        _specialKeyListener = specialKeyListener;
        _vantageDisabler = vantageDisabler;
        _legionZoneDisabler = legionZoneDisabler;
        _fnKeysDisabler = fnKeysDisabler;
        _updateChecker = updateChecker;

        InitializeComponent();

        Closing += MainWindow_Closing;
        Closed += MainWindow_Closed;
        IsVisibleChanged += MainWindow_IsVisibleChanged;
        Loaded += MainWindow_Loaded;
        SourceInitialized += MainWindow_SourceInitialized;
        StateChanged += MainWindow_StateChanged;

#if DEBUG
        _title.Text += Debugger.IsAttached ? " [DEBUGGER ATTACHED]" : " [DEBUG]";
#else
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        if (version is not null && version.IsBeta())
            _title.Text += " [BETA]";
#endif

        if (Log.Instance.IsTraceEnabled)
        {
            _title.Text += " [LOGGING ENABLED]";
            _openLogIndicator.Visibility = Visibility.Visible;
        }

        Title = _title.Text;

        // Listen to Frame navigation events to update window title to current page title
        _rootFrame.Navigated += RootFrame_Navigated;

        // Subscribe to plugin state changed events
        _pluginManager.PluginStateChanged += PluginManager_PluginStateChanged;
    }

    private void RootFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
    {
        // When page navigation is complete, update window title to: App Name - Page Title
        var appName = Resource.ResourceManager.GetString("AppName", Resource.Culture) ?? "Lenovo Legion Toolkit";

        if (e.Content is UiPage page && !string.IsNullOrWhiteSpace(page.Title))
        {
            Title = $"{appName} - {page.Title}";
            _title.Text = $"{appName} - {page.Title}";
        }
        else
        {
            // If there is no page title, only show the app name
            Title = appName;
            _title.Text = appName;
        }
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e) => RestoreSize();

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _contentGrid.Visibility = Visibility.Hidden;

        var mi = await Compatibility.GetMachineInformationAsync();
        var isSupportedLegionMachine = Compatibility.IsSupportedLegionMachine(mi);

        if (!isSupportedLegionMachine)
        {
            _navigationStore.Items.Remove(_dashboardItem);
            _navigationStore.Items.Remove(_automationItem);
            _navigationStore.Items.Remove(_keyboardItem);
            _navigationStore.Items.Remove(_macroItem);

            // If we removed the dashboard, we need to navigate to something else
            _navigationStore.Navigate("windowsOptimization");
        }
        else if (!await KeyboardBacklightPage.IsSupportedAsync())
        {
            _navigationStore.Items.Remove(_keyboardItem);
        }

        // Control WindowsOptimization navigation item visibility based on extension settings
        UpdateNavigationVisibility();

        SmartKeyHelper.Instance.BringToForeground = () => Dispatcher.Invoke(BringToForeground);

        _specialKeyListener.Changed += (_, args) =>
        {
            if (args.SpecialKey == SpecialKey.FnN)
                Dispatcher.Invoke(BringToForeground);
        };

        _contentGrid.Visibility = Visibility.Visible;

        LoadDeviceInfo();
        UpdateIndicators();
        CheckForUpdates();

        InputBindings.Add(new KeyBinding(new ActionCommand(_navigationStore.NavigateToNext), Key.Tab, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new ActionCommand(_navigationStore.NavigateToPrevious), Key.Tab, ModifierKeys.Control | ModifierKeys.Shift));

        var key = (int)Key.D1;
        foreach (var item in _navigationStore.Items.OfType<NavigationItem>())
        {
            if (item.PageTag != null)
                InputBindings.Add(new KeyBinding(new ActionCommand(() => _navigationStore.Navigate(item.PageTag)), (Key)key++, ModifierKeys.Control));
        }
        
        // Set the plugin extensions navigation item text
        if (_pluginExtensionsItem != null)
        {
            _pluginExtensionsItem.Content = Resource.ResourceManager.GetString("MainWindow_NavigationItem_PluginExtensions", Resource.Culture) ?? "Plugin Extensions";
        }

        var trayHelper = new TrayHelper(_navigationStore, BringToForeground, TrayTooltipEnabled);
        await trayHelper.InitializeAsync();
        trayHelper.MakeVisible();
        _trayHelper = trayHelper;
    }

    private async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        SaveSize();

        if (SuppressClosingEventHandler)
            return;

        if (_applicationSettings.Store.MinimizeOnClose)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Minimizing...");

            WindowState = WindowState.Minimized;
            e.Cancel = true;
        }
        else
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Closing...");

            await App.Current.ShutdownAsync(true);
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs args)
    {
        _trayHelper?.Dispose();
        _trayHelper = null;
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Window state changed to {WindowState}");

        switch (WindowState)
        {
            case WindowState.Minimized:
                SetEfficiencyMode(true);
                SendToTray();
                break;
            case WindowState.Normal:
                SetEfficiencyMode(false);
                BringToForeground();
                break;
        }
    }

    private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!IsVisible)
            return;

        CheckForUpdates();
    }

    private void OpenLogIndicator_Click(object sender, MouseButtonEventArgs e) => OpenLog();

    private void OpenLogIndicator_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter and not Key.Space)
            return;

        OpenLog();
    }

    private void DeviceInfoIndicator_Click(object sender, MouseButtonEventArgs e) => ShowDeviceInfoWindow();

    private void DeviceInfoIndicator_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter and not Key.Space)
            return;

        ShowDeviceInfoWindow();
    }

    private void UpdateIndicator_Click(object sender, RoutedEventArgs e) => ShowUpdateWindow();

    private void UpdateIndicator_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter and not Key.Space)
            return;

        ShowUpdateWindow();
    }

    private async void LoadDeviceInfo()
    {
        try
        {
            var mi = await Compatibility.GetMachineInformationAsync();
            _deviceInfoIndicator.Content = mi.Model;
            _deviceInfoIndicator.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to load device info: {ex.Message}", ex);
        }
    }

    private void UpdateIndicators()
    {
        if (DisableConflictingSoftwareWarning)
            return;

        _vantageDisabler.OnRefreshed += (_, e) => Dispatcher.Invoke(() =>
        {
            _vantageIndicator.Visibility = e.Status == SoftwareStatus.Enabled ? Visibility.Visible : Visibility.Collapsed;
        });

        _legionZoneDisabler.OnRefreshed += (_, e) => Dispatcher.Invoke(() =>
        {
            _legionZoneIndicator.Visibility = e.Status == SoftwareStatus.Enabled ? Visibility.Visible : Visibility.Collapsed;
        });

        _fnKeysDisabler.OnRefreshed += (_, e) => Dispatcher.Invoke(() =>
        {
            _fnKeysIndicator.Visibility = e.Status == SoftwareStatus.Enabled ? Visibility.Visible : Visibility.Collapsed;
        });

        Task.Run(async () =>
        {
            _ = await _vantageDisabler.GetStatusAsync().ConfigureAwait(false);
            _ = await _legionZoneDisabler.GetStatusAsync().ConfigureAwait(false);
            _ = await _fnKeysDisabler.GetStatusAsync().ConfigureAwait(false);
        });
    }

    public async void CheckForUpdates(bool manualCheck = false)
    {
        try
        {
            var result = await _updateChecker.CheckAsync(manualCheck);
            if (result is null)
            {
                _updateIndicator.Visibility = Visibility.Collapsed;

                if (manualCheck && WindowState != WindowState.Minimized)
                {
                    switch (_updateChecker.Status)
                    {
                        case UpdateCheckStatus.Success:
                            await SnackbarHelper.ShowAsync(Resource.MainWindow_CheckForUpdates_Success_Title);
                            break;
                        case UpdateCheckStatus.RateLimitReached:
                            await SnackbarHelper.ShowAsync(Resource.MainWindow_CheckForUpdates_Error_Title, Resource.MainWindow_CheckForUpdates_Error_ReachedRateLimit_Message, SnackbarType.Error);
                            break;
                        case UpdateCheckStatus.Error:
                            await SnackbarHelper.ShowAsync(Resource.MainWindow_CheckForUpdates_Error_Title, Resource.MainWindow_CheckForUpdates_Error_Unknown_Message, SnackbarType.Error);
                            break;
                    }
                }
            }
            else
            {
                var versionNumber = result.ToString(3);

                _updateIndicatorText.Text =
                    string.Format(Resource.MainWindow_UpdateAvailableWithVersion, versionNumber);
                _updateIndicator.Visibility = Visibility.Visible;

                if (WindowState == WindowState.Minimized)
                    MessagingCenter.Publish(new NotificationMessage(NotificationType.UpdateAvailable, versionNumber));
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Check for updates failed.", ex);
            
            _updateIndicator.Visibility = Visibility.Collapsed;
        }
    }

    private void RestoreSize()
    {
        if (!_applicationSettings.Store.WindowSize.HasValue)
            return;

        Width = Math.Max(MinWidth, _applicationSettings.Store.WindowSize.Value.Width);
        Height = Math.Max(MinHeight, _applicationSettings.Store.WindowSize.Value.Height);

        ScreenHelper.UpdateScreenInfos();
        var primaryScreen = ScreenHelper.PrimaryScreen;

        if (!primaryScreen.HasValue)
            return;

        var desktopWorkingArea = primaryScreen.Value.WorkArea;
        Left = (desktopWorkingArea.Width - Width) / 2 + desktopWorkingArea.Left;
        Top = (desktopWorkingArea.Height - Height) / 2 + desktopWorkingArea.Top;
    }

    private void SaveSize()
    {
        _applicationSettings.Store.WindowSize = WindowState != WindowState.Normal
            ? new(RestoreBounds.Width, RestoreBounds.Height)
            : new(Width, Height);
        _applicationSettings.SynchronizeStore();
    }

    private void BringToForeground() => WindowExtensions.BringToForeground(this);

    private static void OpenLog()
    {
        try
        {
            if (!Directory.Exists(Folders.AppData))
                return;

            Process.Start("explorer", Log.Instance.LogPath);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to open log.", ex);
        }
    }

    private void ShowDeviceInfoWindow()
    {
        var window = new DeviceInformationWindow { Owner = this };
        window.ShowDialog();
    }

    public void ShowUpdateWindow()
    {
        var window = new UpdateWindow { Owner = this };
        window.ShowDialog();
    }

    public void SendToTray()
    {
        if (!_applicationSettings.Store.MinimizeToTray)
            return;

        SetEfficiencyMode(true);
        Hide();
        ShowInTaskbar = true;
    }

    public void UpdateNavigationVisibility()
    {
        UpdateWindowsOptimizationNavigationVisibility();
        UpdateInstalledPluginsNavigationItems(); // Ensure installed plugins have navigation items on startup
        UpdateNavigationItemsVisibilityFromSettings();
        // UpdatePluginExtensionsNavigationVisibility must be called AFTER UpdateNavigationItemsVisibilityFromSettings
        // to ensure it has the latest visibility settings
        UpdatePluginExtensionsNavigationVisibility();
    }

    private void UpdateNavigationItemsVisibilityFromSettings()
    {
        var visibilitySettings = _applicationSettings.Store.NavigationItemsVisibility;

        SetNavigationItemVisibility(_keyboardItem, "keyboardBacklight", visibilitySettings);
        SetNavigationItemVisibility(_automationItem, "automation", visibilitySettings);
        SetNavigationItemVisibility(_macroItem, "macro", visibilitySettings);
        SetNavigationItemVisibility(_windowsOptimizationItem, "windowsOptimization", visibilitySettings);

        SetNavigationItemVisibility(_aboutItem, "about", visibilitySettings);
    }

    private void SetNavigationItemVisibility(NavigationItem? item, string pageTag, Dictionary<string, bool> visibilitySettings)
    {
        if (item != null)
        {
            var shouldShow = GetNavigationItemVisibility(pageTag, visibilitySettings);
            item.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private bool GetNavigationItemVisibility(string pageTag, Dictionary<string, bool> visibilitySettings)
    {
        // Dashboard and settings must always be visible
        if (pageTag == "dashboard" || pageTag == "settings")
            return true;

        // keyboardBacklight should use the keyboard key
        if (pageTag == "keyboardBacklight")
            pageTag = "keyboard";

        if (visibilitySettings.TryGetValue(pageTag, out var visibility))
            return visibility;

        // Visible by default
        return true;
    }

    private void UpdatePluginExtensionsNavigationVisibility()
    {
        // Control plugin extensions navigation item visibility based on navigation items visibility settings
        // Plugin extensions should be visible by default, just like other navigation items
        // Only controlled by navigation items visibility settings, not by ExtensionsEnabled
        var visibilitySettings = _applicationSettings.Store.NavigationItemsVisibility;
        var shouldShow = GetNavigationItemVisibility("pluginExtensions", visibilitySettings);
        
        if (_pluginExtensionsItem != null)
        {
            _pluginExtensionsItem.Visibility = shouldShow 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }
    }

    public void UpdateWindowsOptimizationNavigationVisibility()
    {
        if (_windowsOptimizationItem == null)
            return;

        // Windows optimization interface is now the default interface, ensure it's in the navigation items list
        var isInItems = _navigationStore.Items.Contains(_windowsOptimizationItem);
        
        if (!isInItems)
        {
            // Find the position of the Macro navigation item and insert after it
            var macroItem = _navigationStore.Items.OfType<NavigationItem>().FirstOrDefault(item => item.PageTag == "macro");
            if (macroItem != null)
            {
                var macroIndex = _navigationStore.Items.IndexOf(macroItem);
                _navigationStore.Items.Insert(macroIndex + 1, _windowsOptimizationItem);
            }
            else
            {
                _navigationStore.Items.Add(_windowsOptimizationItem);
            }
        }
        
        // Visibility is controlled by UpdateNavigationItemsVisibilityFromSettings
    }

    private void PluginManager_PluginStateChanged(object? sender, PluginEventArgs e)
    {
        // When plugin status changes, update navigation bar visibility and plugin navigation items
        Dispatcher.Invoke(() =>
        {
            UpdateInstalledPluginsNavigationItems();
            UpdateNavigationVisibility();
        });
    }

    /// <summary>
    /// Update navigation items for installed plugins
    /// </summary>
    public void UpdateInstalledPluginsNavigationItems()
    {
        try
        {
            // Get all installed plugins (excluding system plugins like Tools and SystemOptimization)
            var installedPluginIds = _pluginManager.GetInstalledPluginIds().ToList();
            var registeredPlugins = _pluginManager.GetRegisteredPlugins().ToList();
            
            // Filter out system plugins
            var pluginsToShow = registeredPlugins
                .Where(p => installedPluginIds.Contains(p.Id, StringComparer.OrdinalIgnoreCase)
                    && !p.IsSystemPlugin)
                .ToList();

            // Remove navigation items for uninstalled plugins
            var pluginIdsToRemove = _pluginNavigationItems.Keys
                .Where(id => !pluginsToShow.Any(p => p.Id == id))
                .ToList();

            foreach (var pluginId in pluginIdsToRemove)
            {
                if (_pluginNavigationItems.TryGetValue(pluginId, out var navItem))
                {
                    _navigationStore.Items.Remove(navItem);
                    _pluginNavigationItems.Remove(pluginId);
                }
            }

            // Add or update navigation items for installed plugins
            foreach (var plugin in pluginsToShow)
            {
                if (!_pluginNavigationItems.ContainsKey(plugin.Id))
                {
                    // Get plugin metadata for version info
                    var pluginMetadata = _pluginManager.GetPluginMetadata(plugin.Id);
                    var pluginDisplayName = plugin.Name;
                    
                    // Create new navigation item for this plugin with icon
                    var navItem = new NavigationItem
                    {
                        Content = pluginDisplayName,
                        PageTag = $"plugin:{plugin.Id}",
                        PageType = typeof(PluginPageWrapper),
                        Tag = pluginMetadata // Store metadata in Tag for later use
                    };
                    
                    // Set icon from plugin's Icon property
                    if (!string.IsNullOrWhiteSpace(plugin.Icon))
                    {
                        navItem.Icon = GetSymbolFromString(plugin.Icon);
                    }
                    else
                    {
                        // Default icon if plugin doesn't specify one
                        navItem.Icon = SymbolRegular.Apps24;
                    }

                    // Register the page tag mapping
                    PluginPageWrapper.RegisterPluginPageTag($"plugin:{plugin.Id}", plugin.Id);

                    // Find the position to insert (after windows optimization item, before plugin extensions item)
                    var insertIndex = -1;
                    if (_windowsOptimizationItem != null && _navigationStore.Items.Contains(_windowsOptimizationItem))
                    {
                        insertIndex = _navigationStore.Items.IndexOf(_windowsOptimizationItem);
                    }
                    else
                    {
                        var macroItem = _navigationStore.Items.OfType<NavigationItem>().FirstOrDefault(item => item.PageTag == "macro");
                        if (macroItem != null)
                        {
                            insertIndex = _navigationStore.Items.IndexOf(macroItem);
                        }
                    }

                    // Insert the navigation item
                    if (insertIndex >= 0)
                    {
                        _navigationStore.Items.Insert(insertIndex + 1, navItem);
                    }
                    else
                    {
                        _navigationStore.Items.Add(navItem);
                    }

                    _pluginNavigationItems[plugin.Id] = navItem;
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error updating plugin navigation items: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Convert string to SymbolRegular enum
    /// </summary>
    private SymbolRegular GetSymbolFromString(string symbolString)
    {
        if (Enum.TryParse<SymbolRegular>(symbolString, out var symbol))
        {
            return symbol;
        }
        return SymbolRegular.Apps24;
    }



    private static unsafe void SetEfficiencyMode(bool enabled)
    {
        var ptr = IntPtr.Zero;

        try
        {
            var priorityClass = enabled
                ? PROCESS_CREATION_FLAGS.IDLE_PRIORITY_CLASS
                : PROCESS_CREATION_FLAGS.NORMAL_PRIORITY_CLASS;
            PInvoke.SetPriorityClass(PInvoke.GetCurrentProcess(), priorityClass);

            var state = new PROCESS_POWER_THROTTLING_STATE
            {
                Version = PInvoke.PROCESS_POWER_THROTTLING_CURRENT_VERSION,
                ControlMask = PInvoke.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
                StateMask = enabled ? PInvoke.PROCESS_POWER_THROTTLING_EXECUTION_SPEED : 0,
            };

            var size = Marshal.SizeOf<PROCESS_POWER_THROTTLING_STATE>();
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(state, ptr, false);

            PInvoke.SetProcessInformation(PInvoke.GetCurrentProcess(),
                PROCESS_INFORMATION_CLASS.ProcessPowerThrottling,
                ptr.ToPointer(),
                (uint)size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
}
