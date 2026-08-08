using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class SettingsPage : UserControl
{
    private readonly IPlatformServices _platformServices;

    private SettingsAppearanceView? _appearanceView;
    private SettingsApplicationBehaviorView? _applicationBehaviorView;
    private SettingsSmartKeysView? _smartKeysView;
    private SettingsDisplayView? _displayView;
    private SettingsUpdateView? _updateView;
    private SettingsPowerView? _powerView;
    private SettingsIntegrationsView? _integrationsView;

    private bool _supportsLenovoHardwareControls;
    private bool _isInitialized;

    public SettingsPage(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
        InitializeComponent();

        Loaded += SettingsPage_Loaded;
        Unloaded += SettingsPage_Unloaded;
    }

    private async void SettingsPage_Loaded(object? sender, RoutedEventArgs e)
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        LoadingSkeleton.IsVisible = true;
        await InitializeNavigationItemsAsync();
        await RefreshAsync();
        LoadingSkeleton.IsVisible = false;
    }

    private void SettingsPage_Unloaded(object? sender, RoutedEventArgs e)
    {
    }

    private async Task InitializeNavigationItemsAsync()
    {
        try
        {
            _supportsLenovoHardwareControls = await _platformServices
                .IsSupportedLegionMachineAsync()
                .ConfigureAwait(true);

            var navigationItems = new List<SettingsNavigationItem>
            {
                new("Appearance", Get("SettingsPage_Navigation_Appearance", "Appearance"), "PaintBrush24"),
                new("Application", Get("SettingsPage_Navigation_Application", "Application"), "Apps24"),
            };

            if (_supportsLenovoHardwareControls)
            {
                navigationItems.Add(new("SmartKeys", Get("SettingsPage_Navigation_SmartKeys", "Smart Keys"), "Keyboard24"));
                navigationItems.Add(new("Display", Get("SettingsPage_Navigation_Display", "Display"), "Desktop24"));
            }

            navigationItems.Add(new("Update", Get("SettingsPage_Update_Title", "Update"), "ArrowSync24"));

            if (_supportsLenovoHardwareControls)
            {
                navigationItems.Add(new("Power", Get("SettingsPage_Power_Title", "Power"), "Battery024"));
            }

            navigationItems.Add(new("Integrations", Get("SettingsPage_Integrations_Title", "Integrations"), "PlugConnected24"));

            SettingsNavigationList.ItemsSource = navigationItems;
            SettingsNavigationList.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing settings navigation: {ex.Message}");
        }
    }

    private async Task RefreshAsync()
    {
        // Create the appearance view first and show it immediately.
        _appearanceView = new SettingsAppearanceView();
        _applicationBehaviorView = new SettingsApplicationBehaviorView();
        _smartKeysView = _supportsLenovoHardwareControls ? new SettingsSmartKeysView() : null;
        _displayView = _supportsLenovoHardwareControls ? new SettingsDisplayView() : null;
        _updateView = new SettingsUpdateView();
        _powerView = _supportsLenovoHardwareControls ? new SettingsPowerView() : null;
        _integrationsView = new SettingsIntegrationsView();

        ShowView("Appearance");
    }

    private void SettingsNavigationList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SettingsNavigationList.SelectedItem is SettingsNavigationItem item)
            ShowView(item.Key);
    }

    private void ShowView(string key)
    {
        var view = key switch
        {
            "Appearance" => _appearanceView,
            "Application" => _applicationBehaviorView,
            "SmartKeys" => _smartKeysView,
            "Display" => _displayView,
            "Update" => _updateView,
            "Power" => _powerView,
            "Integrations" => _integrationsView,
            _ => null,
        };

        if (view is null)
            return;

        SelectedContentHost.Content = view;
    }

    private static string Get(string key, string fallback) =>
        AvaloniaLocalization.GetString(key, fallback);

    private sealed record SettingsNavigationItem(string Key, string Title, string IconIdentifier);
}
