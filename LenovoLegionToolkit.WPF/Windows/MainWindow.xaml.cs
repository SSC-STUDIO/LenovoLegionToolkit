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
    private readonly ApplicationSettings _applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly IPluginManager _pluginManager = IoCContainer.Resolve<IPluginManager>();
    private readonly SpecialKeyListener _specialKeyListener = IoCContainer.Resolve<SpecialKeyListener>();
    private readonly VantageDisabler _vantageDisabler = IoCContainer.Resolve<VantageDisabler>();
    private readonly LegionZoneDisabler _legionZoneDisabler = IoCContainer.Resolve<LegionZoneDisabler>();
    private readonly FnKeysDisabler _fnKeysDisabler = IoCContainer.Resolve<FnKeysDisabler>();
    private readonly UpdateChecker _updateChecker = IoCContainer.Resolve<UpdateChecker>();

    private TrayHelper? _trayHelper;
    private readonly Dictionary<string, NavigationItem> _pluginNavigationItems = new();

    public bool TrayTooltipEnabled { get; init; } = true;
    public bool DisableConflictingSoftwareWarning { get; set; }
    public bool SuppressClosingEventHandler { get; set; }

    public Snackbar Snackbar => _snackbar;

    public MainWindow()
    {
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

        if (!await KeyboardBacklightPage.IsSupportedAsync())
            _navigationStore.Items.Remove(_keyboardItem);

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
        UpdateDonateButtonVisibility();
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

    private void LoadDeviceInfo()
    {
        Task.Run(Compatibility.GetMachineInformationAsync)
            .ContinueWith(mi =>
            {
                _deviceInfoIndicator.Content = mi.Result.Model;
                _deviceInfoIndicator.Visibility = Visibility.Visible;
            }, TaskScheduler.FromCurrentSynchronizationContext());
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

    public void CheckForUpdates(bool manualCheck = false)
    {
        Task.Run(() => _updateChecker.CheckAsync(manualCheck))
            .ContinueWith(updatesAvailable =>
            {
                var result = updatesAvailable.Result;
                if (result is null)
                {
                    HandleNoUpdateAvailable(manualCheck);
                }
                else
                {
                    HandleUpdateAvailable(result);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private void HandleNoUpdateAvailable(bool manualCheck)
    {
        _updateIndicator.Visibility = Visibility.Collapsed;

        if (!manualCheck || WindowState == WindowState.Minimized)
            return;

        ShowUpdateCheckStatusMessage();
    }

    private async void ShowUpdateCheckStatusMessage()
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

    private void HandleUpdateAvailable(Version version)
    {
        var versionNumber = version.ToString(3);
        _updateIndicatorText.Text = string.Format(Resource.MainWindow_UpdateAvailableWithVersion, versionNumber);
        _updateIndicator.Visibility = Visibility.Visible;

        if (WindowState == WindowState.Minimized)
            MessagingCenter.Publish(new NotificationMessage(NotificationType.UpdateAvailable, versionNumber));
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
        UpdateToolsNavigationVisibility();
        UpdateInstalledPluginsNavigationItems(); // Ensure installed plugins have navigation items on startup
        UpdateNavigationItemsVisibilityFromSettings();
        // UpdatePluginExtensionsNavigationVisibility must be called AFTER UpdateNavigationItemsVisibilityFromSettings
        // to ensure it has the latest visibility settings
        UpdatePluginExtensionsNavigationVisibility();
    }

    private void UpdateNavigationItemsVisibilityFromSettings()
    {
        var visibilitySettings = _applicationSettings.Store.NavigationItemsVisibility;

        UpdateNavigationItemVisibility(_keyboardItem, "keyboardBacklight", visibilitySettings);
        UpdateNavigationItemVisibility(_automationItem, "automation", visibilitySettings);
        UpdateNavigationItemVisibility(_macroItem, "macro", visibilitySettings);
        UpdateNavigationItemVisibility(_windowsOptimizationItem, "windowsOptimization", visibilitySettings);
        UpdateNavigationItemVisibility(_toolsItem, "tools", visibilitySettings);
        UpdateNavigationItemVisibility(_donateNavigationItem, "donate", visibilitySettings);
        UpdateNavigationItemVisibility(_aboutItem, "about", visibilitySettings);

        // Update plugin extensions navigation item
        // Note: This is handled separately in UpdatePluginExtensionsNavigationVisibility() 
        // to keep the logic consistent with other navigation items
    }

    private void UpdateNavigationItemVisibility(NavigationItem? item, string pageTag, Dictionary<string, bool> visibilitySettings)
    {
        if (item == null)
            return;

        var shouldShow = GetNavigationItemVisibility(pageTag, visibilitySettings);
        item.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
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

    private void UpdateToolsNavigationVisibility()
    {
        // Tools interface is now the default interface, ensure it's in the navigation items list
        if (_toolsItem != null)
        {
            var isInItems = _navigationStore.Items.Contains(_toolsItem);
            if (!isInItems)
            {
                // Find the position of the Windows optimization navigation item and insert after it; if not found, insert after Macro
                var insertIndex = -1;
                if (_navigationStore.Items.Contains(_windowsOptimizationItem))
                {
                    insertIndex = _navigationStore.Items.IndexOf(_windowsOptimizationItem);
                    _navigationStore.Items.Insert(insertIndex + 1, _toolsItem);
                }
                else
                {
                    var macroItem = _navigationStore.Items.OfType<NavigationItem>().FirstOrDefault(item => item.PageTag == "macro");
                    if (macroItem != null)
                    {
                        insertIndex = _navigationStore.Items.IndexOf(macroItem);
                        _navigationStore.Items.Insert(insertIndex + 1, _toolsItem);
                    }
                    else
                    {
                        _navigationStore.Items.Add(_toolsItem);
                    }
                }
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
    private void UpdateInstalledPluginsNavigationItems()
    {
        try
        {
            var pluginsToShow = GetPluginsToShow();
            RemoveUninstalledPluginNavigationItems(pluginsToShow);
            AddOrUpdatePluginNavigationItems(pluginsToShow);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error updating plugin navigation items: {ex.Message}", ex);
        }
    }

    private List<IPlugin> GetPluginsToShow()
    {
        var installedPluginIds = _pluginManager.GetInstalledPluginIds().ToList();
        var registeredPlugins = _pluginManager.GetRegisteredPlugins().ToList();
        
        return registeredPlugins
            .Where(p => installedPluginIds.Contains(p.Id, StringComparer.OrdinalIgnoreCase) && !p.IsSystemPlugin)
            .ToList();
    }

    private void RemoveUninstalledPluginNavigationItems(List<IPlugin> pluginsToShow)
    {
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
    }

    private void AddOrUpdatePluginNavigationItems(List<IPlugin> pluginsToShow)
    {
        foreach (var plugin in pluginsToShow)
        {
            if (!_pluginNavigationItems.ContainsKey(plugin.Id))
            {
                var navItem = CreatePluginNavigationItem(plugin);
                InsertPluginNavigationItem(navItem);
                _pluginNavigationItems[plugin.Id] = navItem;
            }
        }
    }

    private NavigationItem CreatePluginNavigationItem(IPlugin plugin)
    {
        var pluginMetadata = _pluginManager.GetPluginMetadata(plugin.Id);
        var navItem = new NavigationItem
        {
            Content = plugin.Name,
            PageTag = $"plugin:{plugin.Id}",
            PageType = typeof(PluginPageWrapper),
            Tag = pluginMetadata,
            Icon = GetPluginIcon(plugin)
        };

        PluginPageWrapper.RegisterPluginPageTag($"plugin:{plugin.Id}", plugin.Id);
        return navItem;
    }

    private SymbolRegular GetPluginIcon(IPlugin plugin)
    {
        return !string.IsNullOrWhiteSpace(plugin.Icon) 
            ? GetSymbolFromString(plugin.Icon) 
            : SymbolRegular.Apps24;
    }

    private void InsertPluginNavigationItem(NavigationItem navItem)
    {
        var insertIndex = FindPluginInsertIndex();
        
        if (insertIndex >= 0)
            _navigationStore.Items.Insert(insertIndex + 1, navItem);
        else
            _navigationStore.Items.Add(navItem);
    }

    private int FindPluginInsertIndex()
    {
        if (_toolsItem != null && _navigationStore.Items.Contains(_toolsItem))
            return _navigationStore.Items.IndexOf(_toolsItem);
        
        if (_windowsOptimizationItem != null && _navigationStore.Items.Contains(_windowsOptimizationItem))
            return _navigationStore.Items.IndexOf(_windowsOptimizationItem);
        
        var macroItem = _navigationStore.Items.OfType<NavigationItem>().FirstOrDefault(item => item.PageTag == "macro");
        return macroItem != null ? _navigationStore.Items.IndexOf(macroItem) : -1;
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

    public void UpdateDonateButtonVisibility()
    {
        // Donate button visibility requires both ShowDonateButton setting AND navigation visibility setting
        var visibilitySettings = _applicationSettings.Store.NavigationItemsVisibility;
        if (_donateNavigationItem != null)
        {
            var shouldShow = _applicationSettings.Store.ShowDonateButton 
                && GetNavigationItemVisibility("donate", visibilitySettings);
            _donateNavigationItem.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
        }
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
