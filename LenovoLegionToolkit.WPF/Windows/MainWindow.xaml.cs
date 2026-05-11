using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
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
using Wpf.Ui.Controls;
using UiNavigatedEventArgs = Wpf.Ui.Controls.NavigatedEventArgs;
#if !DEBUG
using System.Reflection;
using LenovoLegionToolkit.Lib.Extensions;
#endif

#pragma warning disable CA1416

namespace LenovoLegionToolkit.WPF.Windows
{
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

    private Snackbar _snackbar = null!;

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

        _snackbar = new Snackbar(_snackbarPresenter)
        {
            MinWidth = 300,
            Icon = new SymbolIcon { Symbol = SymbolRegular.Checkmark24 },
            Effect = new DropShadowEffect
            {
                BlurRadius = 15,
                Direction = 270,
                Opacity = 0.4,
                ShadowDepth = 3,
                Color = Application.Current.Resources["SnackbarShadowColor"] is Color color ? color : Colors.Black
            }
        };

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

        _navigationView.Navigated += NavigationView_Navigated;
        RegisterNavigationItemHandlers();

        // Subscribe to plugin state changed events
        _pluginManager.PluginStateChanged += PluginManager_PluginStateChanged;
    }

    private void NavigationView_Navigated(NavigationView sender, UiNavigatedEventArgs e)
    {
        var appName = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "AppName", "Lenovo Legion Toolkit", Resource.Culture);

        if (e.Page is System.Windows.Controls.Page page && !string.IsNullOrWhiteSpace(page.Title))
        {
            Title = $"{appName} - {page.Title}";
            _title.Text = $"{appName} - {page.Title}";
        }
        else
        {
            Title = appName;
            _title.Text = appName;
        }

        if (e.Page is FrameworkElement element)
            PlayPageTransition(element);
    }

    private static void PlayPageTransition(FrameworkElement element)
    {
        if (Compatibility.IsSmokeLegionSimulationEnabled)
            return;

        element.Opacity = 0;
        element.RenderTransform = new TranslateTransform(0, 8);

        var duration = Application.Current.Resources["AnimationDurationMedium"] is Duration resourceDuration
            ? resourceDuration
            : new Duration(TimeSpan.FromMilliseconds(200));
        var easing = Application.Current.Resources["AnimationEasingCubicOut"] as IEasingFunction;

        var storyboard = new Storyboard();

        var opacityAnimation = new DoubleAnimation(0, 1, duration) { EasingFunction = easing };
        Storyboard.SetTarget(opacityAnimation, element);
        Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));

        var translateAnimation = new DoubleAnimation(8, 0, duration) { EasingFunction = easing };
        Storyboard.SetTarget(translateAnimation, element);
        Storyboard.SetTargetProperty(translateAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

        storyboard.Children.Add(opacityAnimation);
        storyboard.Children.Add(translateAnimation);
        storyboard.Begin();
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e) => RestoreSize();

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _contentGrid.Visibility = Visibility.Hidden;

        var mi = await Compatibility.GetMachineInformationAsync();
        var isSupportedLegionMachine = Compatibility.IsSupportedLegionMachine(mi);

        if (!isSupportedLegionMachine)
        {
            // Keep dashboard visible in compatibility mode for basic functionality
            // _navigationView.MenuItems.Remove(_dashboardItem);
            _navigationView.MenuItems.Remove(_automationItem);
            _navigationView.MenuItems.Remove(_keyboardItem);
            _navigationView.MenuItems.Remove(_macroItem);

            // Navigate to dashboard instead of windowsOptimization for better UX
            _navigationView.Navigate("dashboard", null);
        }
        else if (!await KeyboardBacklightPage.IsSupportedAsync())
        {
            _navigationView.MenuItems.Remove(_keyboardItem);
        }

        UpdateNavigationVisibility();

        if (isSupportedLegionMachine)
            _navigationView.Navigate("dashboard", null);

        SmartKeyHelper.Instance.BringToForeground = () => Dispatcher.Invoke(BringToForeground);

        _specialKeyListener.Changed += (_, args) =>
        {
            if (args.SpecialKey == SpecialKey.FnN)
                Dispatcher.Invoke(BringToForeground);
        };

        _contentGrid.Visibility = Visibility.Visible;

        LoadDeviceInfo();
        UpdateIndicators();
        _ = CheckForUpdates();

        InputBindings.Add(new KeyBinding(new ActionCommand(_navigationView.NavigateToNext), Key.Tab, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new ActionCommand(_navigationView.NavigateToPrevious), Key.Tab, ModifierKeys.Control | ModifierKeys.Shift));

        var key = (int)Key.D1;
        foreach (var item in _navigationView.MenuItems.OfType<NavigationItem>())
        {
            if (item.TargetPageTag != null)
                InputBindings.Add(new KeyBinding(new ActionCommand(() => _navigationView.Navigate(item.TargetPageTag, null)), (Key)key++, ModifierKeys.Control));
        }
        
        // Set the plugin extensions navigation item text
        if (_pluginExtensionsItem != null)
        {
            SetNavigationDisplayContent(_pluginExtensionsItem, LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "MainWindow_NavigationItem_PluginExtensions", "Plugin Extensions", Resource.Culture));
        }

        UpdateNavigationDisplayContent();

        var trayHelper = new TrayHelper(_navigationView, BringToForeground, TrayTooltipEnabled);
        await trayHelper.InitializeAsync();
        trayHelper.MakeVisible();
        _trayHelper = trayHelper;
    }

    private async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Closing started...");

        var suppressClosingEventHandler = SuppressClosingEventHandler;
        var minimizeOnClose = !suppressClosingEventHandler && _applicationSettings.Store.MinimizeOnClose;

        // Cancel before awaiting persistence so the close request cannot slip through.
        if (minimizeOnClose)
            e.Cancel = true;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await SaveSizeAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"SaveSize failed during closing.", ex);
        }
        finally
        {
            stopwatch.Stop();
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"SaveSize completed in {stopwatch.ElapsedMilliseconds}ms");

        try
        {
            if (suppressClosingEventHandler)
                return;

            if (minimizeOnClose)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Minimizing...");

                WindowState = WindowState.Minimized;
                return;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Closing...");

            await App.Current.ShutdownAsync(true);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed while handling window close.", ex);
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs args)
    {
        // Unsubscribe from window events
        Closing -= MainWindow_Closing;
        Closed -= MainWindow_Closed;
        IsVisibleChanged -= MainWindow_IsVisibleChanged;
        Loaded -= MainWindow_Loaded;
        SourceInitialized -= MainWindow_SourceInitialized;
        StateChanged -= MainWindow_StateChanged;

        _navigationView.Navigated -= NavigationView_Navigated;
        UnregisterNavigationItemHandlers();

        // Unsubscribe from plugin manager events
        if (_pluginManager is not null)
            _pluginManager.PluginStateChanged -= PluginManager_PluginStateChanged;

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

        _ = CheckForUpdates();
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

    public async Task CheckForUpdates(bool manualCheck = false)
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

    private async Task SaveSizeAsync()
    {
        _applicationSettings.Store.WindowSize = WindowState != WindowState.Normal
            ? new(RestoreBounds.Width, RestoreBounds.Height)
            : new(Width, Height);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Saving window size asynchronously...");

        await _applicationSettings.SynchronizeStoreAsync().ConfigureAwait(false);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Window size saved asynchronously.");
    }

#if DEBUG
    [Conditional("DEBUG")]
    private void DebugBreakpoint(string location)
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"DEBUG BREAKPOINT: {location}");

        if (Debugger.IsAttached)
            Debugger.Break();
    }
#else
    [Conditional("DEBUG")]
    private void DebugBreakpoint(string location) { }
#endif

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

    private void UpdateNavigationDisplayContent()
    {
        foreach (var item in _navigationView.MenuItems.OfType<NavigationItem>()
                     .Concat(_navigationView.FooterMenuItems.OfType<NavigationItem>()))
        {
            var text = AutomationProperties.GetName(item);
            if (string.IsNullOrWhiteSpace(text))
                text = item.Content?.ToString();

            SetNavigationDisplayContent(item, text);
        }
    }

    private void RegisterNavigationItemHandlers()
    {
        foreach (var item in _navigationView.MenuItems.OfType<NavigationItem>()
                     .Concat(_navigationView.FooterMenuItems.OfType<NavigationItem>()))
        {
            item.Invoked -= NavigationItem_Invoked;
            item.Invoked += NavigationItem_Invoked;
        }
    }

    private void UnregisterNavigationItemHandlers()
    {
        foreach (var item in _navigationView.MenuItems.OfType<NavigationItem>()
                     .Concat(_navigationView.FooterMenuItems.OfType<NavigationItem>()))
        {
            item.Invoked -= NavigationItem_Invoked;
        }
    }

    private void NavigationItem_Invoked(object? sender, EventArgs e)
    {
        if (sender is not NavigationItem { TargetPageTag: { } tag } item)
            return;

        foreach (var navigationItem in _navigationView.MenuItems.OfType<NavigationItem>()
                     .Concat(_navigationView.FooterMenuItems.OfType<NavigationItem>()))
        {
            navigationItem.IsActive = ReferenceEquals(navigationItem, item);
        }

        _navigationView.Navigate(tag, null);
    }

    private static void SetNavigationDisplayContent(NavigationItem item, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        AutomationProperties.SetName(item, text);
        item.ToolTip ??= text;

        var displayText = FormatNavigationDisplayText(text);
        item.DisplayContent = displayText;
        item.SetCurrentValue(ContentControl.ContentProperty, displayText);
    }

    private static string FormatNavigationDisplayText(string text)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return text;

        if (parts.Length == 2)
            return parts[0] + "\n" + parts[1];

        var lineBreakIndex = GetBestNavigationTextLineBreak(parts);
        return string.Join(' ', parts.Take(lineBreakIndex)) + "\n" + string.Join(' ', parts.Skip(lineBreakIndex));
    }

    private static int GetBestNavigationTextLineBreak(string[] parts)
    {
        var bestIndex = 1;
        var bestDelta = int.MaxValue;

        for (var i = 1; i < parts.Length; i++)
        {
            var firstLength = string.Join(' ', parts.Take(i)).Length;
            var secondLength = string.Join(' ', parts.Skip(i)).Length;
            var delta = Math.Abs(firstLength - secondLength);

            if (delta < bestDelta)
            {
                bestIndex = i;
                bestDelta = delta;
            }
        }

        return bestIndex;
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
        var isInItems = _navigationView.MenuItems.Contains(_windowsOptimizationItem);
        
        if (!isInItems)
        {
            // Find the position of the Macro navigation item and insert after it
            var macroItem = _navigationView.MenuItems.OfType<NavigationItem>().FirstOrDefault(item => item.TargetPageTag == "macro");
            if (macroItem != null)
            {
                var macroIndex = _navigationView.MenuItems.IndexOf(macroItem);
                _navigationView.MenuItems.Insert(macroIndex + 1, _windowsOptimizationItem);
            }
            else
            {
                _navigationView.MenuItems.Add(_windowsOptimizationItem);
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
            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"UpdateInstalledPluginsNavigationItems started");
            }

            // Get all installed plugin IDs
            var installedPluginIds = _pluginManager.GetInstalledPluginIds().ToList();
            // Get all registered plugins
            var registeredPlugins = _pluginManager.GetRegisteredPlugins().ToList();
            
            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"  - Installed plugin IDs: [{string.Join(", ", installedPluginIds)}]");
                Log.Instance.Trace($"  - Registered plugins: [{string.Join(", ", registeredPlugins.Select(p => p.Id))}]");
            }
            
            // Only show installed plugins that actually provide a feature page.
            // This prevents blank plugin pages in sidebar navigation.
            var pluginsToShow = registeredPlugins
                .Where(p => installedPluginIds.Contains(p.Id, StringComparer.OrdinalIgnoreCase))
                .Where(ProvidesFeaturePage)
                .ToList();

            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"  - Plugins to show: [{string.Join(", ", pluginsToShow.Select(p => p.Id))}]");
            }

            // Remove navigation items for uninstalled plugins
            var pluginIdsToRemove = _pluginNavigationItems.Keys
                .Where(id => !pluginsToShow.Any(p => p.Id == id))
                .ToList();

            foreach (var pluginId in pluginIdsToRemove)
            {
                if (_pluginNavigationItems.TryGetValue(pluginId, out var navItem))
                {
                    _navigationView.MenuItems.Remove(navItem);
                    _pluginNavigationItems.Remove(pluginId);
                    if (Log.Instance.IsTraceEnabled)
                    {
                        Log.Instance.Trace($"  - Removed navigation item for plugin: {pluginId}");
                    }
                }
            }

            // Add or update navigation items for installed plugins
            foreach (var plugin in pluginsToShow)
            {
                if (!_pluginNavigationItems.ContainsKey(plugin.Id))
                {
                    if (Log.Instance.IsTraceEnabled)
                    {
                        Log.Instance.Trace($"  - Adding navigation item for plugin: {plugin.Id} (Name: {plugin.Name}, IsSystemPlugin: {plugin.IsSystemPlugin})");
                    }

                    // Get plugin metadata for version info
                    var pluginMetadata = _pluginManager.GetPluginMetadata(plugin.Id);
                    var pluginDisplayName = plugin.Name;
                    
                    // Create new navigation item for this plugin with icon
                    var navItem = new NavigationItem
                    {
                        TargetPageTag = $"plugin:{plugin.Id}",
                        TargetPageType = typeof(PluginPageWrapper),
                        Tag = pluginMetadata // Store metadata in Tag for later use
                    };
                    SetNavigationDisplayContent(navItem, pluginDisplayName);
                    
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

                    AutomationProperties.SetAutomationId(navItem, $"PluginNavItem_{plugin.Id}");

                    // Register the page tag mapping
                    PluginPageWrapper.RegisterPluginPageTag($"plugin:{plugin.Id}", plugin.Id);
                    var pluginId = plugin.Id;
                    navItem.Invoked += (_, _) => NavigateToPluginPage(pluginId);
                    navItem.Click += (_, _) => NavigateToPluginPage(pluginId);
                    navItem.PreviewMouseLeftButtonUp += (_, _) => NavigateToPluginPage(pluginId);
                    navItem.KeyDown += (_, e) =>
                    {
                        if (e.Key != Key.Enter && e.Key != Key.Space)
                            return;

                        e.Handled = true;
                        NavigateToPluginPage(pluginId);
                    };

                    // Find the position to insert (after windows optimization item, before plugin extensions item)
                    var insertIndex = -1;
                    if (_windowsOptimizationItem != null && _navigationView.MenuItems.Contains(_windowsOptimizationItem))
                    {
                        insertIndex = _navigationView.MenuItems.IndexOf(_windowsOptimizationItem);
                    }
                    else
                    {
                        var macroItem = _navigationView.MenuItems.OfType<NavigationItem>().FirstOrDefault(item => item.TargetPageTag == "macro");
                        if (macroItem != null)
                        {
                            insertIndex = _navigationView.MenuItems.IndexOf(macroItem);
                        }
                    }

                    // Insert the navigation item
                    if (insertIndex >= 0)
                    {
                        _navigationView.MenuItems.Insert(insertIndex + 1, navItem);
                    }
                    else
                    {
                        _navigationView.MenuItems.Add(navItem);
                    }

                    _pluginNavigationItems[plugin.Id] = navItem;
                }
            }

            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"UpdateInstalledPluginsNavigationItems completed. Total plugin navigation items: {_pluginNavigationItems.Count}");
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

    public bool NavigateToPluginPage(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        try
        {
            var pageTag = $"plugin:{pluginId}";
            PluginPageWrapper.RegisterPluginPageTag(pageTag, pluginId);
            UpdateInstalledPluginsNavigationItems();

            foreach (var item in _navigationView.MenuItems.OfType<NavigationItem>()
                         .Concat(_navigationView.FooterMenuItems.OfType<NavigationItem>()))
            {
                item.IsActive = string.Equals(item.TargetPageTag, pageTag, StringComparison.OrdinalIgnoreCase);
            }

            _navigationView.Navigate(pageTag, null);
            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error navigating to plugin page: {pluginId}", ex);

            return false;
        }
    }

    private static bool ProvidesFeaturePage(IPlugin plugin)
    {
        try
        {
            return PluginPageWrapper.ProvidesFeaturePage(plugin);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to resolve feature-page capability for plugin {plugin.Id}", ex);
            return false;
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
}
